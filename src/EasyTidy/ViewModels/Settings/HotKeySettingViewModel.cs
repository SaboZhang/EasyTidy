using CommunityToolkit.WinUI;
using EasyTidy.Common.Extensions;
using EasyTidy.Common.Model;
using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using EasyTidy.Service;
using EasyTidy.Views.UserControls;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using ModelContextProtocol.Protocol.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.System;
using Windows.UI.Core;
using WinUIEx;
using static EasyTidy.Service.HotkeyService;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace EasyTidy.ViewModels;

public partial class HotKeySettingViewModel : ObservableObject
{
    private readonly IThemeSelectorService _themeSelectorService;

    private readonly ILocalSettingsService _localSettingsService;

    private readonly HotkeyService _hotkeyService;
    private readonly HotkeyActionRouter _hotkeyActionRouter;

    [ObservableProperty]
    private ObservableCollection<HotkeysCollection> _hotkeys;

    [ObservableProperty]
    private bool _isHotkeyEnabled;

    public ObservableCollection<Breadcrumb> Breadcrumbs { get; }

    public HotKeySettingViewModel(IThemeSelectorService themeSelectorService, ILocalSettingsService localSettingsService, HotkeyService hotkeyService, HotkeyActionRouter hotkeyActionRouter)
    {
        _themeSelectorService = themeSelectorService;
        _localSettingsService = localSettingsService;
        _hotkeyService = hotkeyService;
        _hotkeyActionRouter = hotkeyActionRouter;

        Breadcrumbs = new ObservableCollection<Breadcrumb>
        {
            new("Settings".GetLocalized(), typeof(SettingsViewModel).FullName!),
            new("Settings_Hotkey_Header".GetLocalized(), typeof(HotKeySettingViewModel).FullName!),
        };
        _ = LoadHotkeysAsync();

    }

    public async Task LoadHotkeysAsync()
    {
        var hotkeys = await _localSettingsService.LoadSettingsExtAsync<HotkeysCollection>();

        if (hotkeys?.Hotkeys == null)
            return;
        
        IsHotkeyEnabled = hotkeys.Enabled;

        Hotkeys = new ObservableCollection<HotkeysCollection>(new[] { hotkeys });
    }

    [RelayCommand]
    private async Task RegisterUserDefinedHotkeyAsync(Hotkey hotkey)
    {
        var keyString = string.Empty;
        var modifiers = ModifierKeys.None;
        var mainKey = VirtualKey.None;
        var hotkeyId = string.Empty;
        var actionName = string.Empty;
        if (hotkey == null ) return;

        hotkeyId = hotkey.Id;
        actionName = hotkey.CommandName;
        keyString = hotkey.KeyGesture;
        if (_hotkeyService.TryParseHotkey(hotkey.KeyGesture, out var mod, out var mvk))
        {
            mainKey = mvk;
            modifiers = mod;
        }

        // 2. 加载已有的快捷键集合，没有就初始化
        var hotkeys = await _localSettingsService.LoadSettingsExtAsync<HotkeysCollection>()
        ?? new HotkeysCollection { Hotkeys = [] };

        // 3. 查找是否已存在对应Id的快捷键
        var existingHotkey = hotkeys.Hotkeys.FirstOrDefault(h => h.Id == hotkeyId);

        if (existingHotkey != null)
        {
            _hotkeyService.UnregisterHotKey(existingHotkey.Id);
            // 更新已有的快捷键
            existingHotkey.KeyGesture = keyString;
            existingHotkey.CommandName = actionName;
        }
        else
        {
            // 新增快捷键
            hotkeys.Hotkeys.Add(new Hotkey
            {
                Id = hotkeyId,
                KeyGesture = keyString,
                CommandName = actionName
            });
        }

        // 4. 保存修改后的快捷键集合
        await _localSettingsService.SaveSettingsExtAsync(hotkeys);
        await LoadHotkeysAsync();

        // 5. 注册快捷键
        bool success = _hotkeyService.RegisterHotKey(
            hotkeyId,
            mainKey,
            modifiers,
            () => _hotkeyActionRouter.HandleAction(hotkeyId)
        );

        if (!success)
        {
            Logger.Warn($"Failed to register hotkey: {keyString}");
        }
    }

    [RelayCommand]
    private async Task ClearAllHotKeysAsync()
    {
        var hotkeySettings = await _localSettingsService.LoadSettingsExtAsync<HotkeysCollection>();
        if (hotkeySettings != null)
        {
            // 清除所有已注册的快捷键
            foreach (var hotkey in hotkeySettings.Hotkeys)
            {
                _hotkeyService.UnregisterHotKey(hotkey.Id);
            }
            _hotkeyService.Clear();
            // 清空集合
            hotkeySettings.Hotkeys.Clear();
            // 保存修改后的快捷键集合
            await _localSettingsService.SaveSettingsExtAsync(hotkeySettings);
            await LoadHotkeysAsync();
        }
    }

    [RelayCommand]
    private async Task ResetAllHotkeys()
    {
        await _hotkeyService.ResetToDefaultHotkeysAsync();
        await LoadHotkeysAsync();
    }

    [RelayCommand]
    private async Task ResetDefault(string id)
    {
        Hotkeys?.Clear();
        // 清除已注册的快捷键
        _hotkeyService.UnregisterHotKey(id);
        // 重置为默认值
        var defaultHotkey = DefaultHotkeys.GetHotkeyById(id);
        if (defaultHotkey != null)
        {
            var hotkeysCollection = new HotkeysCollection
            {
                Hotkeys = new List<Hotkey> { defaultHotkey }
            };
            Hotkeys = new ObservableCollection<HotkeysCollection>(new[] { hotkeysCollection });
            // 保存修改后的快捷键集合
            await _localSettingsService.SaveSettingsExtAsync(hotkeysCollection);
            _hotkeyService.TryParseHotkey(defaultHotkey.KeyGesture, out var modifiers, out var vk);
            bool success = _hotkeyService.RegisterHotKey(
                defaultHotkey.Id,
                vk,
                modifiers,
                () => _hotkeyActionRouter.HandleAction(defaultHotkey.CommandName));

            if (!success)
            {
                Logger.Warn($"Failed to register hotkey: {defaultHotkey.KeyGesture}");
            }
        }
    }

    /// <summary>
    /// 禁用热键
    /// </summary>
    /// <returns></returns>
    public async Task DisableHotkeysAsync()
    {
        var hotkeys = await _localSettingsService.LoadSettingsExtAsync<HotkeysCollection>();
        if (hotkeys == null) return;

        foreach (var hotkey in hotkeys.Hotkeys)
        {
            _hotkeyService.UnregisterHotKey(hotkey.Id);
        }
        hotkeys.Enabled = !hotkeys.Enabled;
        IsHotkeyEnabled = !IsHotkeyEnabled;
        await _localSettingsService.SaveSettingsExtAsync(hotkeys);
    }

}
