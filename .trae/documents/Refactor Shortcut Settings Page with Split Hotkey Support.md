I will implement the requested "Shortcut" settings page refactoring with the following steps:

1.  **Backend Support (Models & ViewModels)**:
    *   **Modify `AppConfig.cs`**: Add a new `SingleHotkey` property to store the single-key configuration (e.g., "F1").
    *   **Modify `MainViewModel.cs`**: Update the hotkey registration logic to support two distinct hotkeys simultaneously:
        *   `ToggleWindow` (existing): Uses `GlobalHotkey` (e.g., "Alt+Space").
        *   `ToggleWindowSingle` (new): Uses `SingleHotkey` (e.g., "F1") with no modifiers.

2.  **UI Implementation (SettingsView.xaml)**:
    *   **Add Toggle Switch Style**: Create a modern `ToggleSwitchStyle` for the CheckBox to match the "slider switch" requirement.
    *   **Refactor Layout**: Replace the current stack layout with a `Grid` (3 rows, 2 columns) for precise alignment:
        *   **Row 1**: "双击 Ctrl 唤醒" (Left) | Toggle Switch (Right).
        *   **Row 2**: "单键唤醒" (Left) | Input Box for `SingleHotkey` (Right).
        *   **Row 3**: "组合键唤醒" (Left) | Input Box for `GlobalHotkey` (Right).

3.  **Interaction Logic (SettingsView.xaml.cs)**:
    *   **Implement `SingleHotkeyTextBox_PreviewKeyDown`**:
        *   Allow **F1-F12** and other single keys.
        *   **Block** combination keys (Ctrl/Alt/Shift).
        *   Allow **Delete/Backspace** to clear the setting.
    *   **Update `HotkeyTextBox_PreviewKeyDown`**: Ensure the existing combination key input supports the "Delete to clear" requirement.
