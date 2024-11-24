using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace EasyTidy.Model;

public class CoreSettings
{
    public ElementTheme ElementTheme { get; set; } = ElementTheme.Default;
    public BackdropType BackdropType { get; set; } = BackdropType.None;
    public Color BackdropTintColor { get; set; }
    public Color BackdropFallBackColor { get; set; }
}
