---
name: wpf-global-launcher
description: Use when implementing global hotkeys or launcher overlays in WPF applications
---

# WPF Global Launcher Pattern

## Overview

This skill provides patterns for creating global hotkey-triggered overlay windows (launchers, docks, quick actions) in WPF. It covers global hooks, window management, and common pitfalls.

## When to Use

- Adding a global shortcut (e.g., `Alt+Space`) to open an app window.
- Creating "spotlight-like" or "launcher" interfaces.
- Implementing "Quick Action" popups.

## Core Pattern

### 1. Global Hook Service

Use `Gma.System.MouseKeyHook` (or similar low-level hook library) to detect keys even when the app is not focused.

**Important:** Always dispose hooks to prevent memory leaks and ghost inputs.

```csharp
public class GlobalKeyService : IDisposable
{
    private IKeyboardMouseEvents _globalHook;
    public event EventHandler OnLauncherTriggered;

    public void Start()
    {
        _globalHook = Hook.GlobalEvents();
        _globalHook.KeyDown += GlobalHook_KeyDown;
    }

    private void GlobalHook_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Alt && e.KeyCode == Keys.Space) // Example
        {
            OnLauncherTriggered?.Invoke(this, EventArgs.Empty);
            e.Handled = true; // Suppress system handling
        }
    }
    
    public void Stop()
    {
        _globalHook?.Dispose();
    }
}
```

### 2. The Launcher Window (View)

The window should be borderless, topmost, and handle focus loss.

```xml
<Window ...
    WindowStyle="None" 
    AllowsTransparency="True" 
    Background="Transparent"
    Topmost="True"
    ShowInTaskbar="False"
    Deactivated="Window_Deactivated" 
    KeyDown="Window_KeyDown">
```

**Handling Close:**

```csharp
private void Window_Deactivated(object sender, EventArgs e)
{
    Close(); // Close on focus loss
}

private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
{
    if (e.Key == Key.Escape) Close();
}
```

**Ambiguity Warning:**
When mixing Forms (GlobalHook) and WPF, `KeyEventArgs` and `MessageBox` become ambiguous. Always use fully qualified names:
- `System.Windows.Input.KeyEventArgs` (WPF) vs `System.Windows.Forms.KeyEventArgs` (Forms)
- `System.Windows.MessageBox` vs `System.Windows.Forms.MessageBox`

### 3. ViewModel & Execution

Use `Process.Start` with `UseShellExecute = true` for modern Windows compatibility.

```csharp
Process.Start(new ProcessStartInfo("https://google.com") { UseShellExecute = true });
Process.Start(new ProcessStartInfo("excel.exe") { UseShellExecute = true });
```

### 4. Triggering from MainViewModel

Ensure you handle the UI thread properly when the event fires from a hook (which may be on a different thread, though `MouseKeyHook` usually marshals to UI thread, verify it).

```csharp
_keyService.OnLauncherTriggered += (s, e) => 
{
    Application.Current.Dispatcher.Invoke(() => 
    {
        var win = new LauncherWindow();
        win.Show();
        win.Activate();
        win.Focus();
    });
};
```

## Common Pitfalls

1.  **Ghost Windows**: If you `Hide()` instead of `Close()`, ensure you don't leak resources or keep hooks active unnecessarily.
2.  **Focus Stealing**: Use `win.Activate()` and `win.Focus()` immediately after `Show()`.
3.  **Ambiguous Types**: `KeyEventArgs`, `MessageBox`, `Application` exist in both `System.Windows` and `System.Windows.Forms`. Explicitly qualify them.
4.  **Process.Start**: On .NET Core/5+, `UseShellExecute` defaults to `false`. Set it to `true` to open URLs or documents.

