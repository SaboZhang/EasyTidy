using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using EasyTidy.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class GeneralPage : Page
{
    public event EventHandler<RoutedEventArgs> Click;

    //public object CmbPathTypeSelectedItem;
    public GeneralViewModel ViewModel { get; set;}
    public GeneralPage()
    {
        ViewModel = App.GetService<GeneralViewModel>();
        this.InitializeComponent();
        XamlRoot = App.CurrentWindow.Content.XamlRoot;
        //Loaded += General_Loaded;
    }

    //private void General_Loaded(object sender, RoutedEventArgs e)
    //{
    //    if (CmbPathTypeSelectedItem != null)
    //    {
    //        CmbPathType.SelectedItem = CmbPathType.Items.FirstOrDefault(x => ((ComboBoxItem)x).Tag.ToString() == CmbPathTypeSelectedItem.ToString());
    //    }
    //    else
    //    {
    //        CmbPathType.SelectedIndex = 0;
    //    }
    //}

    private void SelectLocation_Click(object sender, RoutedEventArgs e)
    {
        Click?.Invoke(this, e);
    }

    private void CmbPathType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

        var cmb = CmbPathType.Tag.ToString();
        switch (cmb)
        {
            case "Local":
                ViewModel.PathTypeSelectedIndex = true;
                ViewModel.WebDavIsShow = false;
                break;
            case "WebDav":
                ViewModel.PathTypeSelectedIndex = false;
                ViewModel.WebDavIsShow = true;
                break;
            default:
                ViewModel.PathTypeSelectedIndex = false;
                ViewModel.WebDavIsShow = false;
                break;
        }
    }
}
