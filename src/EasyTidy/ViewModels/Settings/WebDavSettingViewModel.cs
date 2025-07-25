using System;
using System.Collections.ObjectModel;
using CommunityToolkit.WinUI;
using EasyTidy.Common.Model;
using EasyTidy.Contracts.Service;

namespace EasyTidy.ViewModels;

public partial class WebDavSettingViewModel : ObservableObject
{
    private readonly ILocalSettingsService _localSettingsService;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public WebDavSettingViewModel(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;

        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new("Settings".GetLocalized(), typeof(SettingsViewModel).FullName!),
            new("Settings_WebDav_Header".GetLocalized(), typeof(WebDavSettingViewModel).FullName!),
        };
    }

    public WebDavUploadViewModel Upload { get; } = new();
    public WebDavBackupViewModel Backup { get; } = new();


    [RelayCommand]
    private async Task OnSaveWebDavUploadAsync()
    {
        var old = Settings.BackupConfig;
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OnSaveWebDavBackupAsync()
    {
        await Task.CompletedTask;
    }

}
