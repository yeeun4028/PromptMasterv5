---
name: "mvvm-vm-splitter"
description: "Splits a large WPF ViewModel into child VMs and wires DI, XAML bindings, and WeakReferenceMessenger. Invoke when refactoring MVVM responsibilities or removing new Service() coupling."
---

# MVVM VM Splitter

## Purpose

Refactor a large WPF `MainViewModel` into small, single-responsibility child ViewModels (e.g. `SidebarViewModel`, `ChatViewModel`), then:

- Register them in DI.
- Update `MainWindow.xaml` bindings to `SidebarVM.*` / `ChatVM.*`.
- Replace direct references/callbacks between components with `WeakReferenceMessenger` messages where UI or cross-VM coordination is needed.
- Keep the app compiling and runnable after every step.

## When to Invoke

Invoke this skill when:

- A ViewModel has too many unrelated responsibilities and needs splitting.
- You are migrating to “Clean Architecture + MVVM” and need dependency-injected child ViewModels.
- You want to stop cross-layer coupling or “ViewModel reaching into View” and instead use messenger events.

## Default Strategy (Safe, Incremental)

1. **Create child ViewModel(s)** with constructor injection and migrate state/commands/methods.
2. **Aggregate in MainViewModel**:
   - Add `public SidebarViewModel SidebarVM { get; }`, `public ChatViewModel ChatVM { get; }`.
   - Keep temporary compatibility forwarding properties if needed to avoid breaking XAML immediately.
3. **Wire DI**:
   - `AddSingleton<SidebarViewModel>()`, `AddSingleton<ChatViewModel>()`, keep `MainViewModel` and `MainWindow`.
   - Map interface registrations (e.g. `IDataService -> WebDavDataService`) explicitly.
4. **Switch XAML bindings**:
   - `Folders` → `SidebarVM.Folders`, `SelectedFolder` → `SidebarVM.SelectedFolder`.
   - `MiniInputText` → `ChatVM.MiniInputText`, etc.
5. **Replace cross-VM/UI interactions with messenger**:
   - Define message types (e.g. focus input, show toast, request window actions).
   - View subscribes to UI-related messages; child VMs send messages instead of touching `MainWindow`.
6. **Verify**:
   - Build in Release.
   - Run quick startup smoke test.

## Message Pattern (Recommended)

- Message types in a small folder like `ViewModels/Messages` (or `Core/Models/Messages` if shared).
- Use records for messages:
  - `record FocusMiniInputMessage();`
  - `record RequestOpenSettingsMessage(int TabIndex);`
- View subscribes on `Loaded` and unsubscribes on `Unloaded`/Dispose (or rely on weak references).

## Non-Goals

- Do not do a full architectural rewrite in one shot.
- Do not introduce new third-party DI frameworks.

