using CommunityToolkit.WinUI;
using EasyTidy.Common.Model;
using EasyTidy.Model;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Windows.System;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace EasyTidy.Views.UserControls;

public sealed partial class ShortcutControl : UserControl
{

    private HashSet<VirtualKey> _heldKeys = new();
    private readonly List<VirtualKey> _pressedSequence = new();

    public event EventHandler<Hotkey> SaveClicked;
    public event EventHandler ResetRequested;
    private bool _enabled;
    private bool _isActive;

    private ShortcutDialogContentControl c = new ShortcutDialogContentControl();
    private ContentDialog shortcutDialog;

    public string Header { get; set; }
    public string Keys { get; set; }

    #region DependencyProperties

    public static readonly DependencyProperty ParametersProperty =
        DependencyProperty.Register(nameof(Parameters), typeof(string), typeof(ShortcutControl), new PropertyMetadata(string.Empty, OnParametersChanged));

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(Enabled), typeof(bool), typeof(ShortcutControl), new PropertyMetadata(false, OnIsActiveChanged));

    public static readonly DependencyProperty HotkeySettingsProperty =
        DependencyProperty.Register(nameof(HotkeySettings), typeof(ObservableCollection<HotkeysCollection>), typeof(ShortcutControl),
            new PropertyMetadata(null, OnHotkeySettingsChanged));

    public static readonly DependencyProperty AllowDisableProperty =
        DependencyProperty.Register(nameof(AllowDisable), typeof(bool), typeof(ShortcutControl),
            new PropertyMetadata(false, OnAllowDisableChanged));

    #endregion

    #region Properties

    public string Parameters
    {
        get => (string)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public bool Enabled
    {
        get => _enabled;
        set => SetValue(IsActiveProperty, value);
    }

    public ObservableCollection<HotkeysCollection> HotkeySettings
    {
        get => (ObservableCollection<HotkeysCollection>)GetValue(HotkeySettingsProperty);
        set => SetValue(HotkeySettingsProperty, value);
    }

    public bool AllowDisable
    {
        get => (bool)GetValue(AllowDisableProperty);
        set => SetValue(AllowDisableProperty, value);
    }

    #endregion

    public ShortcutControl()
    {
        InitializeComponent();

        Loaded += ShortcutControl_Loaded;
        Unloaded += ShortcutControl_Unloaded;

        shortcutDialog = new ContentDialog
        {
            Title = "Activation_Shortcut_Title".GetLocalized(),
            Content = c,
            PrimaryButtonText = "Activation_Shortcut_Save".GetLocalized(),
            SecondaryButtonText = "Activation_Shortcut_Reset".GetLocalized(),
            CloseButtonText = "Activation_Shortcut_Cancel".GetLocalized(),
            DefaultButton = ContentDialogButton.Primary
        };

        shortcutDialog.SecondaryButtonClick += ShortcutDialog_Reset;
        shortcutDialog.RightTapped += ShortcutDialog_Disable;

        AutomationProperties.SetName(EditButton, "Activation_Shortcut_Title".GetLocalized());
        OnAllowDisableChanged(this, null);
        c.KeyDown += OnKeyDown;
        c.KeyUp += OnKeyUp;
    }

    #region EventHandlers

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ShortcutControl control)
        {
            control.UpdatePreviewKeys();
        }
    }

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ShortcutControl control)
        {
            control.EditButton.IsEnabled = (bool)e.NewValue;
            control._enabled = (bool)e.NewValue;
        }
    }

    private static void OnHotkeySettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ShortcutControl control)
        {
            control.UpdatePreviewKeys();
        }
    }

    private static void OnAllowDisableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var me = d as ShortcutControl;
        if (me == null)
        {
            return;
        }
        var description = me.c?.FindDescendant<TextBlock>();
        if (description == null)
        {
            return;
        }

        var newValue = (bool)(e?.NewValue ?? false);

        var text = newValue ? "Activation_Shortcut_With_Disable_Description".GetLocalized() : "Activation_Shortcut_Description".GetLocalized();
        description.Text = text;
    }

    private void ShortcutControl_Loaded(object sender, RoutedEventArgs e)
    {
        shortcutDialog.PrimaryButtonClick += ShortcutDialog_PrimaryButtonClick;
        shortcutDialog.Opened += ShortcutDialog_Opened;
        shortcutDialog.Closing += ShortcutDialog_Closing;
    }

    private void ShortcutControl_Unloaded(object sender, RoutedEventArgs e)
    {
        shortcutDialog.PrimaryButtonClick -= ShortcutDialog_PrimaryButtonClick;
        shortcutDialog.Opened -= ShortcutDialog_Opened;
        shortcutDialog.Closing -= ShortcutDialog_Closing;
    }

    private void ShortcutDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        _isActive = true;
        UpdatePreviewKeys();

    }

    private void ShortcutDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        _isActive = false;
    }

    private void ShortcutDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var keyGesture = string.Join(" + ", c.Keys);

        var newHotkey = new Hotkey
        {
            Id = Parameters,
            KeyGesture = keyGesture,
            CommandName = Parameters
        };

        // var targetCollection = HotkeySettings.FirstOrDefault();

        // if (targetCollection == null)
        // {
        //     targetCollection = new HotkeysCollection();
        //     HotkeySettings.Add(targetCollection);
        // }


        // var existing = targetCollection.Hotkeys.FirstOrDefault(h => h.Id == Parameters);
        // if (existing != null)
        // {
        //     existing.KeyGesture = keyGesture;
        // }
        // else
        // {
        //     targetCollection.Hotkeys.Add(newHotkey);
        // }

        SaveClicked?.Invoke(this, newHotkey);

        shortcutDialog.Hide();
    }

    private void ShortcutDialog_Reset(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        ResetRequested?.Invoke(this, EventArgs.Empty);
        // Reset logic if needed
        UpdatePreviewKeys();
        shortcutDialog.Hide();
    }

    private void ShortcutDialog_Disable(object sender, RightTappedRoutedEventArgs e)
    {
        if (AllowDisable)
        {
            HotkeySettings?.Clear();
            UpdatePreviewKeys();
            shortcutDialog.Hide();
        }
    }

    private async void OpenDialogButton_Click(object sender, RoutedEventArgs e)
    {
        c.Keys = HotkeySettings?.SelectMany(hkc => hkc.Hotkeys)
            .FirstOrDefault(hk => hk.Id == Parameters)?
            .KeyGesture?
            .Split('+', StringSplitOptions.RemoveEmptyEntries)
            .Select(k => (object)k.Trim())
            .Where(k => !string.IsNullOrWhiteSpace(k.ToString()))
            .ToList()
            ?? new List<object>();

        c.IsWarningAltGr = c.Keys.Contains("Ctrl") && c.Keys.Contains("Alt") && !c.Keys.Contains("Win");

        shortcutDialog.XamlRoot = this.XamlRoot;
        shortcutDialog.RequestedTheme = ActualTheme;

        await shortcutDialog.ShowAsync();
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var key = e.Key;


        if (key != VirtualKey.LeftWindows && key != VirtualKey.RightWindows)
        {
            foreach (var winKey in new[] { VirtualKey.LeftWindows, VirtualKey.RightWindows })
            {
                bool isStillDown = (GetAsyncKeyState((int)winKey) & 0x8000) != 0;

                if (!isStillDown)
                {

                    _heldKeys.Remove(winKey);
                    _pressedSequence.Remove(winKey);
                }
            }
        }

        if (_heldKeys.Add(key))
        {
            if (key == VirtualKey.LeftWindows || key == VirtualKey.RightWindows)
            {
                if (!_pressedSequence.Contains(key))
                {
                    _pressedSequence.Add(key);
                }
            }
            else
            {
                _pressedSequence.Add(key);
            }

            UpdateShortcutKeys();
        }

        e.Handled = true;
    }

    private void OnKeyUp(object sender, KeyRoutedEventArgs e)
    {
        var key = e.Key;

        _heldKeys.Remove(key);
        if (key != VirtualKey.LeftWindows && key != VirtualKey.RightWindows)
        {
            _pressedSequence.Remove(key);
        }

        e.Handled = true;
    }

    private void UpdateShortcutKeys()
    {
        string[] modifierOrder = { "Win", "Ctrl", "Alt", "Shift" };

        var formattedKeys = _pressedSequence
            .Select(FormatKey)
            .ToList();

        // 过滤修饰键与普通键
        var modifierKeys = formattedKeys.Where(k => modifierOrder.Contains(k)).ToList();
        var normalKeys = formattedKeys.Except(modifierKeys).ToList();

        // 判断是否是组合键
        bool isCombination = modifierKeys.Count > 0;

        if (isCombination)
        {
            // 只保留最后一个普通键
            if (normalKeys.Count > 1)
                normalKeys = new List<string> { normalKeys.Last() };

            formattedKeys = modifierKeys
                .Concat(normalKeys)
                .OrderBy(k =>
                {
                    int index = Array.IndexOf(modifierOrder, k);
                    return index >= 0 ? index : modifierOrder.Length;
                })
                .ToList();
        }
        else
        {
            // 单键：若不是 F1~F12，则不清除，只设为禁用
            if (normalKeys.Count == 1 && !IsFunctionKey(normalKeys[0]))
            {
                // 保持原输入顺序
                c.Keys = formattedKeys.Cast<object>().ToList();
                DisableKeys();
                return;
            }
        }

        c.Keys = formattedKeys.Cast<object>().ToList();

        bool isValid = IsValidKeyCombination(formattedKeys);

        if (isValid)
        {
            EnableKeys();
        }
        else
        {
            DisableKeys();
        }
    }

    private void EnableKeys()
    {
        shortcutDialog.IsPrimaryButtonEnabled = true;
        c.IsError = false;
    }

    private void DisableKeys()
    {
        shortcutDialog.IsPrimaryButtonEnabled = false;

        var formattedKeys = c.Keys?.Select(k => k.ToString()).ToList() ?? new();
        string[] modifiers = { "Ctrl", "Alt", "Shift", "Win" };
        string[] invalidSingleKeys = { "Tab", "Caps Lock" };

        var modifierKeys = formattedKeys.Where(k => modifiers.Contains(k)).ToList();
        var normalKeys = formattedKeys.Except(modifierKeys).ToList();

        if (modifierKeys.Count == 0)
        {
            // 单键逻辑
            if (normalKeys.Count == 1)
            {
                var key = normalKeys[0];
                // 非 F1~F12，或非法键（如 Tab、Caps Lock）
                if (!IsFunctionKey(key) || invalidSingleKeys.Contains(key))
                {
                    c.IsError = true;
                    return;
                }
            }
        }

        c.IsError = false;
    }

    #endregion

    private void UpdatePreviewKeys()
    {
        var keys = HotkeySettings?.SelectMany(hkc => hkc.Hotkeys)
            .FirstOrDefault(hk => hk.Id == Parameters)?
            .KeyGesture?
            .Split('+', StringSplitOptions.RemoveEmptyEntries)
            .Select(k => (object)k.Trim())
            .Where(k => !string.IsNullOrWhiteSpace(k.ToString()))
            .ToList()
            ?? new List<object>();
        PreviewKeysControl.ItemsSource = keys;
        AutomationProperties.SetHelpText(EditButton, string.Join(", ", keys));
        c.Keys = keys;
    }

    private string FormatKey(VirtualKey key)
    {
        return key switch
        {
            VirtualKey.Control => "Ctrl",
            VirtualKey.Menu => "Alt",
            VirtualKey.LeftWindows => "Win",
            VirtualKey.RightWindows => "Win", // VirtualKey.RightWindows
            VirtualKey.Shift => "Shift",
            VirtualKey.Escape => "Esc",
            VirtualKey.Left => "←",
            VirtualKey.Right => "→",
            VirtualKey.Up => "↑",
            VirtualKey.Down => "↓",
            VirtualKey.Enter => "Enter",
            VirtualKey.CapitalLock => "Caps Lock",
            (VirtualKey)0xBC => ",",    // VK_OEM_COMMA
            (VirtualKey)0xBE => ".",    // VK_OEM_PERIOD
            (VirtualKey)0xBA => ";",    // VK_OEM_1
            (VirtualKey)0xDE => "\"",   // VK_OEM_7
            (VirtualKey)0xBF => "?",    // VK_OEM_2
            (VirtualKey)0xBD => "-",    // VK_OEM_MINUS
            (VirtualKey)0xBB => "+",    // VK_OEM_PLUS
            (VirtualKey)0xDB => "[",    // VK_OEM_4
            (VirtualKey)0xDD => "]",    // VK_OEM_6
            (VirtualKey)0xDC => "\\",   // VK_OEM_5
            (VirtualKey)0xC0 => "~",    // VK_OEM_3
            _ => key.ToString()
        };
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private bool IsValidKeyCombination(List<string> keys)
    {
        if (keys == null || keys.Count == 0)
            return false;

        string[] modifiers = { "Ctrl", "Alt", "Shift", "Win" };
        string[] invalidSingleKeys = { "Tab", "Caps Lock" };

        var modifierKeys = keys.Where(k => modifiers.Contains(k)).ToList();
        var normalKeys = keys.Except(modifierKeys).ToList();

        c.IsWarningAltGr = c.Keys.Contains("Ctrl") && c.Keys.Contains("Alt") && !c.Keys.Contains("Win") && normalKeys.Count > 0;

        if (modifierKeys.Count == 0)
        {
            // 单键，仅允许 F1~F12
            return normalKeys.Count == 1 && IsFunctionKey(normalKeys[0]);
        }

        // 组合键，必须包含一个普通键
        return normalKeys.Count == 1;
    }

    private bool IsFunctionKey(string key)
    {
        return Regex.IsMatch(key, @"^F([1-9]|1[0-2])$");
    }

}
