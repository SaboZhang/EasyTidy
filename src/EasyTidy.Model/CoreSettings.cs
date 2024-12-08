using Microsoft.UI.Xaml;
using Windows.UI;

namespace EasyTidy.Model;

public class CoreSettings
{
    public ElementTheme ElementTheme { get; set; } = ElementTheme.Default;
    public BackdropType BackdropType { get; set; } = BackdropType.Mica;
    public Color BackdropTintColor { get; set; }
    public Color BackdropFallBackColor { get; set; }
    public bool IsFirstRun { get; set; } = true;
}
