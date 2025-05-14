using System;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace EasyTidy.Service;

public class HotkeyService
{
    private readonly Dictionary<string, KeyboardAccelerator> _accelerators = new();

    public void RegisterAccelerator(string id, VirtualKey key, VirtualKeyModifiers modifiers, Action invokedAction)
    {
        if (_accelerators.ContainsKey(id))
            return;

        var accelerator = new KeyboardAccelerator
        {
            Key = key,
            Modifiers = modifiers
        };
        accelerator.Invoked += (s, e) =>
        {
            invokedAction?.Invoke();
            e.Handled = true;
        };

        _accelerators[id] = accelerator;
        var window = App.MainWindow;
        if (window.Content is FrameworkElement root)
        {
            root.KeyboardAccelerators.Add(accelerator);
        }
    }

    public void UnregisterAccelerator(string id)
    {
        if (_accelerators.TryGetValue(id, out var accelerator))
        {
            var window = App.MainWindow;
            if (window.Content is FrameworkElement root)
            {
                root.KeyboardAccelerators.Remove(accelerator);
            }
            _accelerators.Remove(id);
        }
    }

    public void Clear()
    {
        foreach (var acc in _accelerators.Values)
        {
            var window = App.MainWindow;
            if (window.Content is FrameworkElement root)
            {
                root.KeyboardAccelerators.Remove(acc);
            }
        }
        _accelerators.Clear();
    }

    public void RegisterFromGesture(string id, string gesture, Action invokedAction)
    {
        if (_accelerators.ContainsKey(id))
            return;

        if (!TryParseGesture(gesture, out var key, out var modifiers))
            return;

        var accelerator = new KeyboardAccelerator
        {
            Key = key,
            Modifiers = modifiers
        };

        accelerator.Invoked += (s, e) =>
        {
            invokedAction?.Invoke();
            e.Handled = true;
        };

        _accelerators[id] = accelerator;
        var window = App.MainWindow;
        if (window.Content is FrameworkElement root)
        {
            root.KeyboardAccelerators.Add(accelerator);
        }
    }

    private bool TryParseGesture(string gesture, out VirtualKey key, out VirtualKeyModifiers modifiers)
    {
        key = VirtualKey.None;
        modifiers = VirtualKeyModifiers.None;

        if (string.IsNullOrWhiteSpace(gesture)) return false;

        var parts = gesture.Split('+', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var p = part.Trim().ToUpper();
            if (p == "CTRL") modifiers |= VirtualKeyModifiers.Control;
            else if (p == "ALT") modifiers |= VirtualKeyModifiers.Menu;
            else if (p == "SHIFT") modifiers |= VirtualKeyModifiers.Shift;
            else if (Enum.TryParse<VirtualKey>(p, out var k)) key = k;
        }

        return key != VirtualKey.None;
    }

}
