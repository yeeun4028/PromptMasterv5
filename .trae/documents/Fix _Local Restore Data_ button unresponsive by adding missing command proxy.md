The "Local Restore Data" (本地恢复数据) button is unresponsive because the command binding is failing.

**Root Cause:**
- The `SettingsView` uses `MainViewModel` as its DataContext.
- The button is bound to `ManualLocalRestoreCommand`.
- `ManualLocalRestoreCommand` exists in `SettingsViewModel` but is missing from `MainViewModel`.
- Unlike other commands (like `ManualRestoreCommand` or `ShowRestoreConfirmCommand`), `MainViewModel` does not have a proxy method for `ManualLocalRestoreCommand`, causing the binding to fail silently.

**Proposed Changes:**
1.  **Modify `ViewModels/MainViewModel.cs`**:
    - Add a proxy method `ManualLocalRestore` with `[RelayCommand]` attribute.
    - This method will delegate the execution to `SettingsVM.ManualLocalRestoreCommand`.
    - This follows the existing pattern in `MainViewModel` where other settings-related commands are proxied.

**Verification:**
- After applying the fix, the `ManualLocalRestoreCommand` will be available on `MainViewModel`.
- The binding in `SettingsView.xaml` will resolve correctly.
- Clicking the button will trigger the logic in `SettingsViewModel`.
