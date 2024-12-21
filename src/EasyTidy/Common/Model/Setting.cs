namespace EasyTidy.Common.Model;

public class Setting
{
    public string Path { get; }

    public string UniqueId { get; }

    public string Header { get; }

    public string Description { get; }

    public string Glyph { get; }

    public bool HasToggleSwitch { get; }

    public bool HasSettingsProvider { get; }

    public Setting(string path, string uniqueId, string header, string description, string glyph, bool hasToggleSwitch, bool hasSettingsProvider)
    {
        Path = path;
        UniqueId = uniqueId;
        Header = header;
        Description = description;
        Glyph = glyph;
        HasToggleSwitch = hasToggleSwitch;
        HasSettingsProvider = hasSettingsProvider;

    }

}
