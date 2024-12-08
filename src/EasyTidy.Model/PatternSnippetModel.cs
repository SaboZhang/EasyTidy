namespace EasyTidy.Model;

public class PatternSnippetModel
{
    public string Code { get; }
    public string Description { get; }

    public PatternSnippetModel(string code, string description)
    {
        Code = code;
        Description = description;
    }
}
