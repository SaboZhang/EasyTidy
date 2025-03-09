using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.XWPF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using EasyTidy.Model;
using System.Text;
using UglyToad.PdfPig;

namespace EasyTidy.Util;

public class FileReader
{
    /// <summary>
    /// 读取 TXT 文件内容
    /// </summary>
    /// <param name="filePath">TXT 文件路径</param>
    /// <returns>文件内容字符串</returns>
    public static string ReadTxt(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("文件未找到", filePath);

        return File.ReadAllText(filePath);
    }

    /// <summary>
    /// 读取 Excel 文件内容
    /// </summary>
    /// <param name="filePath">Excel 文件路径</param>
    /// <returns>包含每个单元格内容的二维列表</returns>
    public static List<List<string>> ReadExcel(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("文件未找到", filePath);

        IWorkbook workbook;
        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            if (Path.GetExtension(filePath).Equals(".xls", StringComparison.OrdinalIgnoreCase))
            {
                workbook = new HSSFWorkbook(fileStream); // 处理 .xls 文件
            }
            else if (Path.GetExtension(filePath).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                workbook = new XSSFWorkbook(fileStream); // 处理 .xlsx 文件
            }
            else
            {
                throw new NotSupportedException("不支持的文件格式");
            }
        }

        var data = new List<List<string>>();
        ISheet sheet = workbook.GetSheetAt(0); // 获取第一个工作表

        for (int row = 0; row <= sheet.LastRowNum; row++)
        {
            IRow currentRow = sheet.GetRow(row);
            if (currentRow != null)
            {
                var rowData = new List<string>();
                for (int col = 0; col < currentRow.LastCellNum; col++)
                {
                    NPOI.SS.UserModel.ICell cell = currentRow.GetCell(col);
                    rowData.Add(cell?.ToString() ?? string.Empty);
                }
                data.Add(rowData);
            }
        }
        return data;
    }

    /// <summary>
    /// 读取 Word 文件内容
    /// </summary>
    /// <param name="filePath">Word 文件路径</param>
    /// <returns>文件内容字符串</returns>
    public static string ReadWord(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("文件未找到", filePath);

        if (!Path.GetExtension(filePath).Equals(".docx", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("仅支持 .docx 格式的文件");

        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            XWPFDocument document = new XWPFDocument(fileStream);
            using (StringWriter sw = new StringWriter())
            {
                // 读取段落内容
                foreach (var paragraph in document.Paragraphs)
                {
                    sw.WriteLine(paragraph.ParagraphText);
                }

                // 读取表格内容
                foreach (var table in document.Tables)
                {
                    foreach (var row in table.Rows)
                    {
                        foreach (var cell in row.GetTableCells())
                        {
                            sw.Write(cell.GetText() + "\t");
                        }
                        sw.WriteLine();
                    }
                }
                return sw.ToString();
            }
        }
    }

    public static FileType GetFileType(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("文件未找到", filePath);

        byte[] buffer = new byte[4];
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            fs.Read(buffer, 0, buffer.Length);
        }

        string fileHeader = BitConverter.ToString(buffer).Replace("-", string.Empty);

        // 判断文件头
        switch (fileHeader)
        {
            case "25504446": // %PDF
                return FileType.Pdf;
            case "D0CF11E0": // DOC/XLS
                {
                    // 进一步判断是 DOC 还是 XLS
                    // 读取文件的第 512 个字节
                    byte[] subBuffer = new byte[2];
                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        fs.Seek(512, SeekOrigin.Begin);
                        fs.Read(subBuffer, 0, subBuffer.Length);
                    }
                    string subHeader = BitConverter.ToString(subBuffer).Replace("-", string.Empty);
                    if (subHeader == "EC00" || subHeader == "0908")
                        return FileType.Xls;
                    else
                        return FileType.Doc;
                }
            case "504B0304": // DOCX/XLSX (ZIP)
                {
                    // 进一步判断是 DOCX 还是 XLSX
                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    using (BinaryReader reader = new BinaryReader(fs))
                    {
                        fs.Seek(0, SeekOrigin.Begin);
                        byte[] zipHeader = reader.ReadBytes((int)fs.Length);
                        string content = Encoding.UTF8.GetString(zipHeader);
                        if (content.Contains("[Content_Types].xml") && content.Contains("word/"))
                            return FileType.Docx;
                        else if (content.Contains("[Content_Types].xml") && content.Contains("xl/"))
                            return FileType.Xlsx;
                    }
                    break;
                }
            default:
                break;
        }

        // 如果文件头不匹配已知类型，进一步检查是否为 TXT 文件
        if (IsTextFile(filePath))
            return FileType.Txt;

        return FileType.Unknown;
    }

    private static bool IsTextFile(string filePath)
    {
        const int sampleSize = 1024;
        byte[] buffer = new byte[sampleSize];
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            int bytesRead = fs.Read(buffer, 0, sampleSize);
            for (int i = 0; i < bytesRead; i++)
            {
                byte b = buffer[i];
                if (b > 0x7F && b < 0xA0) // 非 ASCII 字符
                    return false;
            }
        }
        return true;
    }

    public static string ExtractTextFromPdf(string pdfPath)
    {
        StringBuilder text = new StringBuilder();

        using (PdfDocument document = PdfDocument.Open(pdfPath))
        {
            foreach (var page in document.GetPages())
            {
                text.AppendLine(page.Text);
            }
        }

        return text.ToString();
    }
}
