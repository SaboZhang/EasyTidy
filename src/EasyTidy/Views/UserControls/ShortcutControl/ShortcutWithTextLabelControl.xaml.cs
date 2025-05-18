using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views.UserControls;

public sealed partial class ShortcutWithTextLabelControl : UserControl
{
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ShortcutWithTextLabelControl), new PropertyMetadata(default(string)));

    public List<object> Keys {
        get => (List<object>)GetValue(KeysProperty);
        set => SetValue(KeysProperty, value);
    }

    public static readonly DependencyProperty KeysProperty = DependencyProperty.Register("Keys", typeof(List<object>), typeof(ShortcutWithTextLabelControl), new PropertyMetadata(default(string)));

    public ShortcutWithTextLabelControl()
    {
        this.InitializeComponent();
    }

}
