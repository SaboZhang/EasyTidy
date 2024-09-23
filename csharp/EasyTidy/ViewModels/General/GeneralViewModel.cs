using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTidy.ViewModels;

public partial class GeneralViewModel : ObservableRecipient
{
    [ObservableProperty]
    private object cmbPathTypeSelectedItem;

    [ObservableProperty]
    private bool pathTypeSelectedIndex = false;

    [ObservableProperty]
    private bool webDavIsShow = false;

    [ObservableProperty]
    public string floderPath;


    [RelayCommand]
    private Task OnSelectPathType()
    {
        var cmb = (CmbPathTypeSelectedItem as ComboBoxItem)?.Tag.ToString();
        switch (cmb)
        {
            case "Local":
                PathTypeSelectedIndex = true;
                WebDavIsShow = false;
                break;
            case "WebDav":
                PathTypeSelectedIndex = false;
                WebDavIsShow = true;
                break;
            default:
                PathTypeSelectedIndex = false;
                WebDavIsShow = false;
                break;
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OnSelectPath()
    {
        try
        {
            var folder = await FileAndFolderPickerHelper.PickSingleFolderAsync(App.CurrentWindow);
            if (folder is not null)
            {
                FloderPath = folder.Path;
            }
            else
            {
                FloderPath = "WebDAV";
            }
            
        }
        catch (Exception ex)
        {
            Logger.Error($"ServerViewModel: OnSelectMediaPath 异常信息 {ex}");
        }
    }
}
