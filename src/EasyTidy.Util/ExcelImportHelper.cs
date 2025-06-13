using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using CommunityToolkit.WinUI;
using EasyTidy.Log;
using EasyTidy.Model;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;

namespace EasyTidy.Util;

public static class ExcelImportHelper
{
    /// <summary>
    /// 通用 Excel 导入方法
    /// </summary>
    /// <typeparam name="T">目标模型类型</typeparam>
    /// <param name="filePath">Excel 文件路径</param>
    /// <param name="startRowIndex">起始行索引（默认跳过前两行）</param>
    /// <param name="mapFunc">每行到模型的转换函数</param>
    /// <returns>模型列表</returns>
    public static List<T> ImportFromExcel<T>(
        string filePath,
        int startRowIndex,
        Func<IRow, T?> mapFunc)
        where T : class
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("找不到 Excel 文件", filePath);

        IWorkbook workbook;
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        workbook = Path.GetExtension(filePath).ToLower() switch
        {
            ".xls" => new HSSFWorkbook(fs),
            ".xlsx" => new XSSFWorkbook(fs),
            _ => throw new NotSupportedException("只支持 .xls 和 .xlsx 格式")
        };

        var result = new List<T>();
        var sheet = workbook.GetSheetAt(0);
        if (sheet == null) return result;

        int[] requiredIndexes = GetRequiredColumnIndexes<OrchestrationTask>(ColumnHeaders);

        for (int i = startRowIndex; i <= sheet.LastRowNum; i++)
        {
            var row = sheet.GetRow(i);
            if (row == null || IsRequiredCellsEmpty(row, requiredIndexes)) continue;

            try
            {
                var model = mapFunc(row);
                if (model != null)
                    result.Add(model);
            }
            catch
            {
                LogService.Logger.Error($"导入 Excel 时出错，行号：{i + 1}，请检查数据格式。");
                continue;
            }
        }

        return result;
    }

    private static bool IsRequiredCellsEmpty(IRow row, int[] requiredColumnIndexes)
    {
        foreach (int index in requiredColumnIndexes)
        {
            var cell = row.GetCell(index);
            if (cell != null && !string.IsNullOrWhiteSpace(cell.ToString()))
            {
                return false;
            }
        }
        return true;
    }

    private static int[] GetRequiredColumnIndexes<T>(string[] columnHeaders)
    {
        var requiredIndexes = new List<int>();
        var props = typeof(T).GetProperties();

        for (int i = 0; i < columnHeaders.Length; i++)
        {
            var header = columnHeaders[i];

            var matchingProp = props.FirstOrDefault(prop =>
            {
                var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();
                return displayAttr != null && displayAttr.Name == header;
            });

            if (matchingProp != null && Attribute.IsDefined(matchingProp, typeof(RequiredAttribute)))
            {
                requiredIndexes.Add(i);
            }
        }

        return requiredIndexes.ToArray();
    }

    public static string GetCellValue(this IRow row, int cellIndex)
    {
        var cell = row.GetCell(cellIndex);
        if (cell == null) return string.Empty;

        return cell.CellType switch
        {
            CellType.String => cell.StringCellValue,
            CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                                ? string.Format("{0:yyyy-MM-dd HH:mm:ss}", cell.DateCellValue)
                                : cell.NumericCellValue.ToString(),
            CellType.Boolean => cell.BooleanCellValue.ToString(),
            CellType.Formula => cell.ToString(), // 或 cell.CellFormula
            CellType.Blank => string.Empty,
            _ => cell.ToString()
        };
    }

    public static void CreateTemplate(IWorkbook workbook, ISheet sheet)
    {
        CreateDescriptionRow(workbook, sheet);
        CreateHeaderRow(workbook, sheet);
        var displayNames = GetLocalizedEnumOptions<OperationMode>();
        AddDropDownList(sheet, displayNames, 2, 65533, 4, 4); // 第3列（索引3），行2开始（第3行）到第101行
        AddDropDownList(sheet, ActionOptions, 2, 65533, 3, 3); // 第6列（索引3），行2开始（第3行）到第101行
    }

    /// <summary>
    /// Excel 列头
    /// </summary>
    private static readonly string[] ColumnHeaders = new[]
    {
        "任务组名",
        "任务名称",
        "处理规则",
        "是否正则",
        "操作方式",
        "源路径",
        "目标路径"
    };

    private static string[] GetLocalizedEnumOptions<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T))
                   .Cast<Enum>()
                   .Where(e => !e.ToString().Equals("None", StringComparison.OrdinalIgnoreCase))
                   .Select(EnumHelper.GetDisplayName)
                   .ToArray();
    }

    private static readonly string[] ActionOptions = new[]
    {
        "Y", "N"
    };

    private static void AddDropDownList(ISheet sheet, string[] items, int firstRow, int lastRow, int firstCol, int lastCol)
    {
        var helper = new XSSFDataValidationHelper((XSSFSheet)sheet);
        var constraint = helper.CreateExplicitListConstraint(items);
        var addressList = new CellRangeAddressList(firstRow, lastRow, firstCol, lastCol);
        var validation = helper.CreateValidation(constraint, addressList);
        validation.ShowErrorBox = true;
        sheet.AddValidationData(validation);
    }

    private static void CreateDescriptionRow(IWorkbook workbook, ISheet sheet)
    {
        var row = sheet.CreateRow(0);
        row.HeightInPoints = 180;

        var cell = row.CreateCell(0);
        cell.SetCellValue("模板填写说明：\n" +
                          "1. 任务组名：可选，若不填写则默认为导入文件名加当前日期（例如：导入文件名为'测试模板'则组名为'测试模板2025-06-12'）。\n" +
                          "2. 任务名称：必填，任务的唯一标识。\n" +
                          "3. 处理规则：必选，规则与单个添加一致。\n" +
                          "4. 是否正则：必选，是否将规则按照正则表达式进行处理，选项为：Y/N。\n" +
                          "5. 操作方式：必填，支持的操作方式为添加单个任务时的所有规则，并且跟当前应用设置的语言有关（例如应用设置的英文，则操作方式填写Move等）。\n" +
                          "6. 源路径：选填，任务源文件/文件夹路径，为空时只能通过拖拽窗口执行任务。（路径格式为：D:\\db\\测试）\n" +
                          "7. 目标路径：必填，任务目标文件/文件夹路径。（路径格式为：D:\\db\\测试）\n");

        // 设置样式
        var style = workbook.CreateCellStyle();
        var font = workbook.CreateFont();
        font.FontHeightInPoints = 12;
        style.SetFont(font);

        style.WrapText = true;
        // 设置背景色
        style.FillForegroundColor = IndexedColors.LightYellow.Index; // 推荐色：淡黄色
        style.FillPattern = FillPattern.SolidForeground;

        // 设置边框
        SetBorders(style);

        cell.CellStyle = style;
        sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, ColumnHeaders.Length - 1));
    }

    // 创建表头行及样式
    private static void CreateHeaderRow(IWorkbook workbook, ISheet sheet)
    {
        var header = sheet.CreateRow(1);

        var greenStyle = workbook.CreateCellStyle();
        var redStyle = workbook.CreateCellStyle();
        var font = workbook.CreateFont();
        font.IsBold = true;
        font.FontHeightInPoints = 12;

        // 设置绿色背景（表示可选项）
        greenStyle.FillForegroundColor = IndexedColors.LightGreen.Index;
        greenStyle.FillPattern = FillPattern.SolidForeground;
        greenStyle.SetFont(font);
        SetBorders(greenStyle);

        // 设置红色背景（表示必填项）
        redStyle.FillForegroundColor = IndexedColors.LightOrange.Index; // 或 LightOrange/LightRed 更柔和
        redStyle.FillPattern = FillPattern.SolidForeground;
        redStyle.SetFont(font);
        SetBorders(redStyle);

        for (int i = 0; i < ColumnHeaders.Length; i++)
        {
            var cell = header.CreateCell(i);
            cell.SetCellValue(ColumnHeaders[i]);

            // 设置第 0 和第 5 列为绿色，其余为红色
            if (i == 0 || i == 5)
                cell.CellStyle = greenStyle;
            else
                cell.CellStyle = redStyle;

            sheet.SetColumnWidth(i, 20 * 256);
        }
    }

    private static void SetBorders(ICellStyle style)
    {
        style.BorderTop = BorderStyle.Medium;
        style.BorderBottom = BorderStyle.Medium;
        style.BorderLeft = BorderStyle.Medium;
        style.BorderRight = BorderStyle.Medium;
    }
}
