---
name: "mvvm-stage5-cleanup"
description: "Fixes code-behind references after MVVM split, removes MainViewModel compatibility shims, and verifies WeakReferenceMessenger registrations. Invoke after XAML bindings are moved to child VMs."
---

# MVVM Stage 5 Cleanup

## Goal

After updating XAML bindings to `SidebarVM.*` / `ChatVM.*` and introducing `WeakReferenceMessenger`, this skill:

- Updates `MainWindow.xaml.cs` code-behind references so it no longer uses legacy `MainViewModel` shim properties.
- Cleans `MainViewModel.cs` by removing compatibility forwarding properties and wrapper commands that are no longer referenced.
- Verifies `WeakReferenceMessenger` registrations and ensures build/startup smoke test passes.

## When to Invoke

Invoke when:

- The app has already been split into child VMs and XAML bindings point to them.
- You want to remove temporary adapter properties like `Folders`, `MiniInputText`, etc. from `MainViewModel`.
- You see code-behind still referencing `ViewModel.MiniInputText` or `ViewModel.Folders`.

## Procedure

1. **Scan code-behind**
   - Find `ViewModel.<OldProperty>` references in `MainWindow.xaml.cs`.
   - Replace with `ViewModel.ChatVM.*` or `ViewModel.SidebarVM.*` based on responsibility.
   - Decide authoritative ownership:
     - Chat: `MiniInputText`, `SearchResults`, `SelectedSearchItem`, `IsSearchPopupOpen`, `IsAiProcessing`, pinned prompts.
     - Sidebar: `Folders`, `SelectedFolder`, drag-drop handler.
     - Global: `Files`, `SelectedFile`, `Config`, `LocalConfig`, `IsFullMode` remain in `MainViewModel` unless explicitly moved.

2. **Clean MainViewModel**
   - Delete forwarding properties used only for compatibility.
   - Keep global state and messenger registration.
   - Remove wrapper commands only after confirming XAML and code-behind no longer reference them.

3. **Verify messenger**
   - Ensure the constructor registers all relevant message types and handlers still match current behavior.
   - Prefer registering once in constructor (or a `RegisterMessages()` helper).

4. **Validate**
   - Build in Release.
   - Run a brief startup smoke test.

## Output

- A compiling solution with code-behind updated, no compatibility shims left in `MainViewModel`, and messenger subscriptions verified.

