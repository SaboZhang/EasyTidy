using System;
using System.Collections.Generic;
using System.IO;
using EasyTidy.Log;
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

        for (int i = startRowIndex; i <= sheet.LastRowNum; i++)
        {
            var row = sheet.GetRow(i);
            if (row == null) continue;

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

    private static readonly string[] ColumnHeaders = new[]
    {
        "任务组名",
        "任务名称",
        "处理规则",
        "操作方式",
        "源路径",
        "目标路径"
    };

    public static void CreateDescriptionRow(IWorkbook workbook, ISheet sheet)
    {
        var row = sheet.CreateRow(0);
        row.HeightInPoints = 130;

        var cell = row.CreateCell(0);
        cell.SetCellValue("模板填写说明：\n" +
                          "1. 任务组名：可选，若不填写则默认为导入文件名加当前日期（例如：导入文件名为'测试模板'则组名为'测试模板2025-06-12'）。\n" +
                          "2. 任务名称：必填，任务的唯一标识。\n" +
                          "3. 处理规则：必选，规则与单个添加一致。\n" +
                          "4. 操作方式：必填，支持的操作方式为添加单个任务时的所有规则，并且跟当前应用设置的语言有关（例如应用设置的英文，则操作方式填写Move等）。\n" +
                          "5. 源路径：选填，任务源文件夹路径，为空时只能通过拖拽窗口执行任务。（路径格式为：D:\\db\\测试）\n" +
                          "6. 目标路径：必填，任务目标文件夹路径。（路径格式为：D:\\db\\测试）\n");

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
    public static void CreateHeaderRow(IWorkbook workbook, ISheet sheet)
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

            // 设置第 0 和第 3 列为绿色，其余为红色
            if (i == 0 || i == 4)
                cell.CellStyle = greenStyle;
            else
                cell.CellStyle = redStyle;

            sheet.SetColumnWidth(i, 20 * 256);
        }
    }

    private static void SetBorders(ICellStyle style)
    {
        style.BorderTop = BorderStyle.Thin;
        style.BorderBottom = BorderStyle.Thin;
        style.BorderLeft = BorderStyle.Thin;
        style.BorderRight = BorderStyle.Thin;
    }
}
