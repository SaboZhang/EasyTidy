using CommunityToolkit.WinUI;
using EasyTidy.Common.Model;
using EasyTidy.Contracts.Service;
using EasyTidy.Model;
using EasyTidy.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using static EasyTidy.Service.HotkeyService;

namespace EasyTidy.ViewModels;

public partial class HotKeySettingViewModel : ObservableObject
{
    private readonly IThemeSelectorService _themeSelectorService;

    private readonly ILocalSettingsService _localSettingsService;

    private readonly HotkeyService _hotkeyService;
    private readonly HotkeyActionRouter _hotkeyActionRouter;

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
    }

    [RelayCommand]
    private async Task RegisterUserDefinedHotkeyAsync()
    {
        // 1. 接收用户自定义快捷键信息（示例占位）
        VirtualKey key = VirtualKey.None;
        VirtualKeyModifiers modifiers = VirtualKeyModifiers.None;
        var hotkeyId = string.Empty;
        var actionName = string.Empty;

        // 2. 加载已有的快捷键集合，没有就初始化
        var hotkeys = await _localSettingsService.LoadSettingsAsync<HotkeysCollection>()
        ?? new HotkeysCollection { Hotkeys = [] };

        // 3. 查找是否已存在对应Id的快捷键
        var existingHotkey = hotkeys.Hotkeys.FirstOrDefault(h => h.Id == hotkeyId);

        if (existingHotkey != null)
        {
            // 更新已有的快捷键
            existingHotkey.KeyGesture = ToGestureString(key, modifiers);
            existingHotkey.CommandName = actionName;
        }
        else
        {
            // 新增快捷键
            hotkeys.Hotkeys.Add(new Hotkey
            {
                Id = hotkeyId,
                KeyGesture = ToGestureString(key, modifiers),
                CommandName = actionName
            });
        }

        // 4. 保存修改后的快捷键集合
        await _localSettingsService.SaveSettingsAsync(hotkeys);

        // 5. 注册快捷键
        bool success = _hotkeyService.RegisterHotKey(
            hotkeyId,
            key,
            ConvertToModifierKeys(modifiers),
            () => _hotkeyActionRouter.HandleAction(hotkeyId)
        );

        if (!success)
        {
            Logger.Warn($"Failed to register hotkey: {ToGestureString(key, modifiers)}");
        }
    }

    private static ModifierKeys ConvertToModifierKeys(VirtualKeyModifiers vkModifiers)
    {
        ModifierKeys result = ModifierKeys.None;

        if (vkModifiers.HasFlag(VirtualKeyModifiers.Control))
            result |= ModifierKeys.Control;

        if (vkModifiers.HasFlag(VirtualKeyModifiers.Shift))
            result |= ModifierKeys.Shift;

        if (vkModifiers.HasFlag(VirtualKeyModifiers.Menu)) // Alt
            result |= ModifierKeys.Alt;

        if (vkModifiers.HasFlag(VirtualKeyModifiers.Windows))
            result |= ModifierKeys.Win;

        return result;
    }

    [RelayCommand]
    private async Task ClearAllHotKeysAsync()
    {
        var hotkeySettings = await _localSettingsService.LoadSettingsAsync<HotkeysCollection>("Hotkeys.json");
        if (hotkeySettings != null)
        {
            // 清除所有已注册的快捷键
            foreach (var hotkey in hotkeySettings.Hotkeys)
            {
                _hotkeyService.UnregisterHotKey(hotkey.Id);
            }

            // 清空集合
            hotkeySettings.Hotkeys.Clear();
            // 保存修改后的快捷键集合
            await _localSettingsService.SaveSettingsAsync(hotkeySettings);
        }
    }

    [RelayCommand]
    private async Task ResetAllHotkeys()
    {
        await _hotkeyService.ResetToDefaultHotkeysAsync();
    }

}
