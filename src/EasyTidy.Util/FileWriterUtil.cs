using EasyTidy.Model;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Writer;

namespace EasyTidy.Util;

public class FileWriterUtil: IFontResolver
{
    /// <summary>
    /// 将对象的文本内容写入 PDF 文件，自动进行换行和分页处理。
    /// </summary>
    public static void WriteObjectToPdf(object obj, string filePath)
    {
        ValidateInputs(obj, filePath);
        string content = obj.ToString();

        try
        {
            var builder = new PdfDocumentBuilder();

            // 使用 PdfPig 内置的 PageSize 枚举（例如 A4）
            PageSize pageSize = PageSize.A4;
            (double pageWidth, double pageHeight) = GetPageDimensions(pageSize);
            const double margin = 40;
            double contentWidth = pageWidth - 2 * margin;

            double fontSize = 12;
            double lineHeight = fontSize * 1.2;
            string fontResourcePath = Path.Combine(Constants.ExecutePath, "Themes", "Fonts", "AlibabaPuHuiTi-3-55-Regular.ttf");
            var font = LoadFont(builder, fontResourcePath);

            // 按换行符拆分文本为段落
            string[] paragraphs = SplitParagraphs(content);

            PdfPageBuilder page = builder.AddPage(pageSize);
            double currentY = pageHeight - margin;

            foreach (var paragraph in paragraphs)
            {
                IEnumerable<string> lines = SplitMixedText(paragraph, fontSize, contentWidth);

                foreach (var line in lines)
                {
                    if (currentY < margin + lineHeight)
                    {
                        page = builder.AddPage(pageSize);
                        currentY = pageHeight - margin;
                    }
                    page.AddText(line, fontSize, new PdfPoint(margin, currentY), font);
                    currentY -= lineHeight;
                }

                // 段落之间增加额外间距
                currentY -= lineHeight * 0.5;
            }

            byte[] documentBytes = builder.Build();
            File.WriteAllBytes(filePath, documentBytes);
        }
        catch (Exception ex)
        {
            throw new IOException($"写入 PDF 文件失败: {ex.Message}", ex);
        }
    }

    #region 辅助方法

    private static void ValidateInputs(object obj, string filePath)
    {
        if (obj == null)
            throw new ArgumentException("对象不能为 null", nameof(obj));
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("文件路径不能为空", nameof(filePath));
        if (string.IsNullOrEmpty(obj.ToString()))
            throw new ArgumentException("无法从对象提取有效字符串", nameof(obj));
    }

    private static string[] SplitParagraphs(string content)
    {
        return content.Contains('\n') || content.Contains('\r')
        ? content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
        : new[] { content };
    }

    /// <summary>
    /// 按照中英混排文本进行智能换行，保证一行满了才换行
    /// </summary>
    private static IEnumerable<string> SplitMixedText(string text, double fontSize, double contentWidth)
    {
        var lines = new List<string>();
        int start = 0;
        int lastSpaceIndex = -1; // 记录最后一个空格的位置，优先按空格换行

        double width = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            bool isEnglish = IsEnglishCharacter(c);
            double charWidth = isEnglish ? (c == ' ' ? fontSize * 0.3 : fontSize * 0.6) : fontSize * 0.8;

            if (c == ' ')
            {
                lastSpaceIndex = i; // 记录空格位置
            }
            // 判断文本是否为纯英文
            bool isPureEnglish = text.All(c => c <= 127); // ASCII 码小于等于 127 视为纯英文

            double maxWidth = isPureEnglish ? contentWidth : contentWidth - charWidth * 8; // 纯英文不缩减宽度
            // 判断是否超过最大宽度
            if (width + charWidth > maxWidth)
            {
                int end = (lastSpaceIndex > start) ? lastSpaceIndex : i; // 优先按空格换行
                lines.Add(text.Substring(start, end - start));
                start = (lastSpaceIndex > start) ? lastSpaceIndex + 1 : i; // 跳过空格
                lastSpaceIndex = -1; // 重置空格索引
                width = 0; // 重新计算新行的宽度
                i = start - 1; // 重新遍历当前字符
            }
            else
            {
                width += charWidth;
            }
        }

        // 添加最后一行
        if (start < text.Length)
        {
            lines.Add(text.Substring(start));
        }

        return lines;
    }

    /// <summary>
    /// 判断字符是否为英文字符（包括字母、数字、符号）
    /// </summary>
    private static bool IsEnglishCharacter(char c)
    {
        return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || char.IsDigit(c) || c == ' ' || char.IsPunctuation(c);
    }

    /// <summary>
    /// 从嵌入资源中加载 TrueType 字体，并添加到 PDF 文档构建器中。
    /// </summary>
    private static PdfDocumentBuilder.AddedFont LoadFont(PdfDocumentBuilder builder, string fontResourcePath)
    {
        byte[] fontBytes = GetEmbeddedResourceBytes(fontResourcePath);
        return builder.AddTrueTypeFont(fontBytes);
    }

    /// <summary>
    /// 从嵌入资源中读取文件的字节流。
    /// </summary>
    private static byte[] GetEmbeddedResourceBytes(string resourceName)
    {
        if (!File.Exists(resourceName))
        {
            throw new FileNotFoundException($"字体文件未找到：{resourceName}");
        }

        return File.ReadAllBytes(resourceName);
    }

    /// <summary>
    /// 根据 PdfPig 的 PageSize 枚举返回页面尺寸（单位：PDF 点）。
    /// 目前仅支持 PageSize.A4，其尺寸为 595 x 842。
    /// </summary>
    private static (double width, double height) GetPageDimensions(PageSize pageSize)
    {
        switch (pageSize)
        {
            case PageSize.A4:
                return (595, 842);
            default:
                throw new NotSupportedException($"不支持的页面尺寸：{pageSize}");
        }
    }

    #endregion

    /// <summary>
    /// 将对象内容写入 PDF 文件（支持中文、自动分页）
    /// </summary>
    /// <param name="obj">要写入的对象（可转换为字符串）</param>
    /// <param name="outputFilePath">输出的 PDF 文件路径</param>
    public static void WriteObjectToPdf(object obj, string outputFilePath, string title = "总结文档")
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        if (string.IsNullOrWhiteSpace(outputFilePath)) throw new ArgumentException("输出路径不能为空", nameof(outputFilePath));

        string content = obj.ToString();

        PdfDocument document = new PdfDocument();
        document.Info.Title = title;

        // 加载系统中文字体
        string fontName = "SimHei";

        XFont font = new XFont(fontName, 12, XFontStyleEx.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
        double margin = 40;
        double lineHeight = font.GetHeight();

        // 文字布局宽度
        double usableWidth = 0;
        double usableHeight = 0;

        // 当前页面与绘图对象
        PdfPage page = null;
        XGraphics gfx = null;
        double y = 0;

        // 分段绘制文本（支持自动换行）
        using (StringReader reader = new StringReader(content))
        {
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                // 首次或分页创建页面
                if (page == null)
                {
                    page = document.AddPage();
                    gfx = XGraphics.FromPdfPage(page);
                    usableWidth = page.Width.Point - 2 * margin;
                    usableHeight = page.Height.Point - 2 * margin;
                    y = margin;
                }

                var formattedLines = SplitLineByWidth(gfx, line, font, usableWidth);

                foreach (var wrappedLine in formattedLines)
                {
                    if (y + lineHeight > page.Height.Point - margin)
                    {
                        page = document.AddPage();
                        gfx.Dispose();
                        gfx = XGraphics.FromPdfPage(page);
                        y = margin;
                    }

                    gfx.DrawString(wrappedLine, font, XBrushes.Black, new XRect(margin, y, usableWidth, lineHeight), XStringFormats.TopLeft);
                    y += lineHeight;
                }
            }
        }

        // 保存文件
        document.Save(outputFilePath);
            document.Close();
        }

    /// <summary>
    /// 按页面宽度自动拆分字符串（支持中文）
    /// </summary>
    private static string[] SplitLineByWidth(XGraphics gfx, string text, XFont font, double maxWidth)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        double width = 0;

        foreach (char ch in text)
        {
            var w = gfx.MeasureString(ch.ToString(), font).Width;
            if (width + w > maxWidth)
            {
                result.Add(sb.ToString());
                sb.Clear();
                width = 0;
            }

            sb.Append(ch);
            width += w;
        }

        if (sb.Length > 0)
        {
            result.Add(sb.ToString());
        }

        return result.ToArray();
    }

}
