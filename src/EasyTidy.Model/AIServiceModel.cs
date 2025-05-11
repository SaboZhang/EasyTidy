namespace EasyTidy.Model;

public class RequestModel(string text, string language = "", string filePath = "")
{
    public string Text { get; set; } = text;
    public string FilePath { get; set; } = filePath;
    public string Language { get; set; } = language;
}
