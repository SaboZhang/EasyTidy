using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views.UserControls;

public sealed partial class SettingsPageControl : UserControl
{
    public SettingsPageControl()
    {
        this.InitializeComponent();
        PrimaryLinks = new ObservableCollection<PageLink>();
        SecondaryLinks = new ObservableCollection<PageLink>();
    }

    public string ModuleTitle
    {
        get { return (string)GetValue(ModuleTitleProperty); }
        set { SetValue(ModuleTitleProperty, value); }
    }

    public string ModuleDescription
    {
        get => (string)GetValue(ModuleDescriptionProperty);
        set => SetValue(ModuleDescriptionProperty, value);
    }

    public string ModuleImageSource
    {
        get => (string)GetValue(ModuleImageSourceProperty);
        set => SetValue(ModuleImageSourceProperty, value);
    }

    public ObservableCollection<PageLink> PrimaryLinks
    {
        get => (ObservableCollection<PageLink>)GetValue(PrimaryLinksProperty);
        set => SetValue(PrimaryLinksProperty, value);
    }

    public string SecondaryLinksHeader
    {
        get { return (string)GetValue(SecondaryLinksHeaderProperty); }
        set { SetValue(SecondaryLinksHeaderProperty, value); }
    }

    public ObservableCollection<PageLink> SecondaryLinks
    {
        get => (ObservableCollection<PageLink>)GetValue(SecondaryLinksProperty);
        set => SetValue(SecondaryLinksProperty, value);
    }

    public object ModuleContent
    {
        get { return (object)GetValue(ModuleContentProperty); }
        set { SetValue(ModuleContentProperty, value); }
    }

    public static readonly DependencyProperty ModuleTitleProperty = DependencyProperty.Register("ModuleTitle", typeof(string), typeof(SettingsPageControl), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty ModuleDescriptionProperty = DependencyProperty.Register("ModuleDescription", typeof(string), typeof(SettingsPageControl), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty ModuleImageSourceProperty = DependencyProperty.Register("ModuleImageSource", typeof(string), typeof(SettingsPageControl), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty PrimaryLinksProperty = DependencyProperty.Register("PrimaryLinks", typeof(ObservableCollection<PageLink>), typeof(SettingsPageControl), new PropertyMetadata(new ObservableCollection<PageLink>()));
    public static readonly DependencyProperty SecondaryLinksHeaderProperty = DependencyProperty.Register("SecondaryLinksHeader", typeof(string), typeof(SettingsPageControl), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty SecondaryLinksProperty = DependencyProperty.Register("SecondaryLinks", typeof(ObservableCollection<PageLink>), typeof(SettingsPageControl), new PropertyMetadata(new ObservableCollection<PageLink>()));
    public static readonly DependencyProperty ModuleContentProperty = DependencyProperty.Register("ModuleContent", typeof(object), typeof(SettingsPageControl), new PropertyMetadata(new Grid()));

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        PrimaryLinksControl.Focus(FocusState.Programmatic);
    }
}
