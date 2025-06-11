using System;
using System.Collections.Generic;
using System.IO;
using EasyTidy.Log;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
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
}
