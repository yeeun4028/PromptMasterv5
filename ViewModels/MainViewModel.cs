using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Input;
using GongSolutions.Wpf.DragDrop;
using NHotkey;
using NHotkey.Wpf;
using PromptMasterv5.Models;
using PromptMasterv5.Services;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.IO;

using InputMode = PromptMasterv5.Models.InputMode;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using IDropTarget = GongSolutions.Wpf.DragDrop.IDropTarget;

namespace PromptMasterv5.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IDataService _dataService;
        private readonly GlobalKeyService _keyService;

        // ★★★ 新增：浏览器自动化服务 ★★★
        private readonly BrowserAutomationService _browserService;

        private bool _isCreatingFile = false;
        private DispatcherTimer _timer;
        private DateTime _lastSyncTime = DateTime.Now;
        private IntPtr _previousWindowHandle = IntPtr.Zero;

        [ObservableProperty] private AppConfig config;
        [ObservableProperty] private LocalSettings localConfig = new LocalSettings();
        [ObservableProperty] private bool isFullMode = true;
        [ObservableProperty] private string miniInputText = "";
        [ObservableProperty] private bool isSearchPopupOpen = false;
        [ObservableProperty] private ObservableCollection<PromptItem> searchResults = new();
        [ObservableProperty] private PromptItem? selectedSearchItem;
        [ObservableProperty] private bool isMiniVarsExpanded = false;

        [ObservableProperty] private bool isSettingsOpen = false;
        [ObservableProperty] private int selectedSettingsTab = 0;
        [ObservableProperty] private string syncTimeDisplay = "Now";
        [ObservableProperty] private ICollectionView? filesView;
        public IDropTarget FolderDropHandler { get; private set; }
        [ObservableProperty] private bool isNavigationVisible = true;
        [ObservableProperty] private ObservableCollection<FolderItem> folders = new();

        [ObservableProperty] private FolderItem? selectedFolder;

        [ObservableProperty] private ObservableCollection<PromptItem> files = new();
        [ObservableProperty] private PromptItem? selectedFile;
        [ObservableProperty] private bool isEditMode;
        [ObservableProperty] private ObservableCollection<VariableItem> variables = new();
        [ObservableProperty] private bool hasVariables;
        [ObservableProperty] private string additionalInput = "";

        public MainViewModel()
        {
            Config = ConfigService.Load();
            LocalConfig = LocalConfigService.Load();
            UpdateGlobalHotkey();

            _keyService = new GlobalKeyService();
            _keyService.OnDoubleCtrlDetected += (s, e) => Application.Current.Dispatcher.Invoke(() => ToggleMainWindow());

            if (Config.EnableDoubleCtrl) try { _keyService.Start(); } catch { }

            // ★★★ 新增：初始化并启动浏览器监听服务 ★★★
            _browserService = new BrowserAutomationService();
            _browserService.OnTargetSiteMatched += BrowserService_OnTargetSiteMatched;
            _browserService.Start();

            _dataService = new WebDavDataService();
            FolderDropHandler = new FolderDropHandler(this);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => UpdateTimeDisplay();
            _timer.Start();

            _ = InitializeAsync();
        }

        // ★★★ 新增：处理浏览器匹配事件 ★★★
        private void BrowserService_OnTargetSiteMatched(object? sender, EventArgs e)
        {
            // 必须在 UI 线程执行
            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow == null) return;

                // 触发前提检查：
                // 1. 软件必须处于 极简模式 (IsFullMode == false)
                // 2. 极简窗口必须处于 显示状态 (Visibility == Visible)
                if (!IsFullMode && mainWindow.Visibility == Visibility.Visible)
                {
                    // 记录刚才的浏览器窗口句柄，方便之后 SmartFocus 能够发送回去
                    CaptureForegroundWindow();

                    // 强制拉回焦点
                    mainWindow.Activate();
                    mainWindow.Focus();
                    mainWindow.Topmost = true; // 确保置顶

                    if (mainWindow is MainWindow win)
                    {
                        win.MiniInputBox.Focus();
                    }
                }
            });
        }

        [SupportedOSPlatform("windows")]
        public void UpdateGlobalHotkey()
        {
            try
            {
                string hotkeyStr = Config.GlobalHotkey;
                if (string.IsNullOrEmpty(hotkeyStr)) return;
                ModifierKeys modifiers = ModifierKeys.None;
                if (hotkeyStr.Contains("Ctrl")) modifiers |= ModifierKeys.Control;
                if (hotkeyStr.Contains("Alt")) modifiers |= ModifierKeys.Alt;
                if (hotkeyStr.Contains("Shift")) modifiers |= ModifierKeys.Shift;
                if (hotkeyStr.Contains("Win")) modifiers |= ModifierKeys.Windows;
                string keyStr = hotkeyStr.Split('+').Last().Trim();
                if (Enum.TryParse(keyStr, out Key key))
                {
                    try { HotkeyManager.Current.Remove("ToggleWindow"); } catch { }
                    HotkeyManager.Current.AddOrReplace("ToggleWindow", key, modifiers, OnGlobalHotkeyTriggered);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"热键注册失败: {ex.Message}"); }
        }

        private void OnGlobalHotkeyTriggered(object? sender, HotkeyEventArgs e) => ToggleMainWindow();

        partial void OnMiniInputTextChanged(string value)
        {
            if (value.StartsWith("/") || value.StartsWith("、"))
            {
                string keyword = value.Length > 1 ? value.Substring(1) : "";
                PerformSearch(keyword);
                IsSearchPopupOpen = true;
            }
            else IsSearchPopupOpen = false;
            ParseVariablesRealTime(value);
        }

        private void PerformSearch(string keyword)
        {
            SearchResults.Clear();
            if (string.IsNullOrWhiteSpace(keyword)) { foreach (var file in Files.Take(10)) SearchResults.Add(file); }
            else
            {
                var lowerKey = keyword.ToLower();
                var matches = Files.Where(f => f.Title.ToLower().Contains(lowerKey)).Take(10);
                foreach (var m in matches) SearchResults.Add(m);
            }
            if (SearchResults.Count > 0) SelectedSearchItem = SearchResults.FirstOrDefault();
        }

        [RelayCommand]
        private void ConfirmSearchResult()
        {
            if (SelectedSearchItem != null) { MiniInputText = SelectedSearchItem.Content ?? ""; IsSearchPopupOpen = false; }
        }

        private void ParseVariablesRealTime(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                Variables.Clear(); HasVariables = false; IsMiniVarsExpanded = false; return;
            }
            var matches = Regex.Matches(content, @"\{\{(.*?)\}\}");
            var newVarNames = matches.Cast<Match>().Select(m => m.Groups[1].Value.Trim()).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList();
            for (int i = Variables.Count - 1; i >= 0; i--) if (!newVarNames.Contains(Variables[i].Name)) Variables.RemoveAt(i);
            foreach (var name in newVarNames) if (!Variables.Any(v => v.Name == name)) Variables.Add(new VariableItem { Name = name });
            HasVariables = Variables.Count > 0;
            if (!IsFullMode)
            {
                IsMiniVarsExpanded = HasVariables;
            }
        }

        [RelayCommand] private void ToggleWindowMode() => IsFullMode = !IsFullMode;
        [RelayCommand] private void EnterFullMode() { if (!IsFullMode) IsFullMode = true; }
        [RelayCommand] private void ExitFullMode() { if (IsFullMode) IsFullMode = false; else Application.Current.MainWindow?.Hide(); }

        private string CompileContent()
        {
            string finalContent;
            if (IsFullMode) finalContent = SelectedFile?.Content ?? "";
            else finalContent = MiniInputText;

            if (HasVariables)
            {
                foreach (var variable in Variables) finalContent = finalContent.Replace("{{" + variable.Name + "}}", variable.Value ?? "");
            }

            if (IsFullMode && !string.IsNullOrWhiteSpace(AdditionalInput))
            {
                if (!string.IsNullOrWhiteSpace(finalContent)) finalContent += "\n";
                finalContent += AdditionalInput;
            }

            return finalContent;
        }

        public async Task SendBySmartFocus()
        {
            string content = CompileContent();
            await ExecuteSendAsync(content, InputMode.SmartFocus);
            if (IsFullMode) AdditionalInput = "";
            else MiniInputText = "";
        }

        public async Task SendByCoordinate()
        {
            string content = CompileContent().TrimEnd();
            await ExecuteSendAsync(content, InputMode.CoordinateClick);
            if (IsFullMode) AdditionalInput = "";
            else MiniInputText = "";
        }

        private async Task ExecuteSendAsync(string content, InputMode targetMode)
        {
            if (string.IsNullOrWhiteSpace(content)) return;
            var window = Application.Current.MainWindow;

            if (window != null) window.Hide();

            await InputSender.SendAsync(content, targetMode, LocalConfig, _previousWindowHandle);

            if (!IsFullMode)
            {
                await Task.Delay(100);
                if (window != null)
                {
                    window.Show();
                    window.WindowState = WindowState.Normal;
                    window.Activate();
                    window.Topmost = true;
                    window.Focus();
                    if (window is MainWindow mainWin)
                    {
                        await mainWin.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            mainWin.MiniInputBox.Focus();
                        }), DispatcherPriority.Render);
                    }
                }
            }
        }

        [RelayCommand]
        private async Task SendFromMini(string modeStr)
        {
            if (modeStr == "Coordinate") await SendByCoordinate();
            else await SendBySmartFocus();
        }

        // --- 辅助逻辑 (CRUD等) ---

        partial void OnSelectedFolderChanged(FolderItem? value)
        {
            FilesView?.Refresh();
            SelectedFile = null;
        }

        partial void OnSelectedFileChanged(PromptItem? oldValue, PromptItem? newValue)
        {
            if (oldValue != null) oldValue.PropertyChanged -= SelectedFile_PropertyChanged;
            if (newValue != null)
            {
                newValue.PropertyChanged += SelectedFile_PropertyChanged;
                if (IsFullMode) ParseVariablesRealTime(newValue.Content ?? "");
            }
            else if (IsFullMode) { Variables.Clear(); HasVariables = false; AdditionalInput = ""; }

            if (_isCreatingFile) return;
            IsEditMode = false;
            RequestSave();
        }

        private void SelectedFile_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PromptItem.Content) && IsFullMode) ParseVariablesRealTime(SelectedFile?.Content ?? "");
            RequestSave();
        }

        public void ToggleMainWindow()
        {
            var window = Application.Current.MainWindow;
            if (window == null) return;
            if (window.Visibility == Visibility.Visible) window.Hide();
            else
            {
                CaptureForegroundWindow();
                if (window.Visibility != Visibility.Visible) IsFullMode = false;
                window.Show(); window.Activate(); window.Focus();
                if (!IsFullMode && window is MainWindow mainWin)
                {
                    mainWin.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        mainWin.MiniInputBox.Focus();
                    }), DispatcherPriority.Render);
                }
            }
        }

        public void CaptureForegroundWindow()
        {
            var handle = NativeMethods.GetForegroundWindow();
            if (handle != IntPtr.Zero) _previousWindowHandle = handle;
        }

        // ... 命令 ...
        [RelayCommand] private void CreateFolder() { var f = new FolderItem { Name = $"新建文件夹 {Folders.Count + 1}" }; Folders.Add(f); SelectedFolder = f; RequestSave(); }
        [RelayCommand] private void CreateFile() { if (SelectedFolder == null) return; _isCreatingFile = true; var f = new PromptItem { Title = "新文档", Content = "# 新文档", FolderId = SelectedFolder.Id, LastModified = DateTime.Now }; Files.Add(f); SelectedFile = f; IsEditMode = true; RequestSave(); _isCreatingFile = false; }
        [RelayCommand] private void DeleteFile(PromptItem? i) { var t = i ?? SelectedFile; if (t != null) { Files.Remove(t); if (SelectedFile == t) SelectedFile = null; RequestSave(); } }
        [RelayCommand] private void ChangeFolderIcon(FolderItem f) { /* ... */ }
        [RelayCommand] private void RenameFolder(FolderItem f) { /* ... */ }
        [RelayCommand] private void ChangeFileIcon(PromptItem f) { /* ... */ }
        [RelayCommand] private void OpenSettings() { Config = ConfigService.Load(); LocalConfig = LocalConfigService.Load(); SelectedSettingsTab = 0; IsSettingsOpen = true; }
        [RelayCommand] private void SaveSettings() { ConfigService.Save(Config); LocalConfigService.Save(LocalConfig); UpdateGlobalHotkey(); if (Config.EnableDoubleCtrl) try { _keyService.Start(); } catch { } else _keyService.Stop(); IsSettingsOpen = false; }
        [RelayCommand] private void SelectSettingsTab(string s) { if (int.TryParse(s, out int i)) SelectedSettingsTab = i; }
        public void ReorderFolders(int o, int n) { Folders.Move(o, n); RequestSave(); }
        public void MoveFileToFolder(PromptItem f, FolderItem t) { if (f == null || t == null || f.FolderId == t.Id) return; f.FolderId = t.Id; FilesView?.Refresh(); if (SelectedFile == f) SelectedFile = null; RequestSave(); }
        [RelayCommand] private void ToggleNavigation() => IsNavigationVisible = !IsNavigationVisible;
        [RelayCommand] private void ToggleEditMode() { IsEditMode = !IsEditMode; if (!IsEditMode) RequestSave(); }
        [RelayCommand] private void CopyCompiledText() { /* ... */ }
        [RelayCommand] private async Task SendDirectPrompt() { await SendFromMini("SmartFocus"); }
        [RelayCommand] private async Task SendCombinedInput() { await SendFromMini("SmartFocus"); }
        [RelayCommand] private async Task ManualBackup() { ConfigService.Save(Config); try { await _dataService.SaveAsync(Folders, Files); _lastSyncTime = DateTime.Now; MessageBox.Show("备份成功"); } catch (Exception e) { MessageBox.Show(e.Message); } }
        [RelayCommand] private async Task ManualRestore() { /* ... */ }

        [RelayCommand]
        private async Task ImportMarkdownFiles()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择 Markdown 文件 (支持多选)",
                Filter = "Markdown Files (*.md;*.txt)|*.md;*.txt|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                var targetFolder = SelectedFolder ?? Folders.FirstOrDefault();
                if (targetFolder == null)
                {
                    targetFolder = new FolderItem { Name = "导入的提示词" };
                    Folders.Add(targetFolder);
                    SelectedFolder = targetFolder;
                }

                int count = 0;
                foreach (var filePath in dialog.FileNames)
                {
                    try
                    {
                        string title = Path.GetFileNameWithoutExtension(filePath);
                        string content = await File.ReadAllTextAsync(filePath);

                        var newItem = new PromptItem
                        {
                            Title = title,
                            Content = content,
                            FolderId = targetFolder.Id,
                            LastModified = DateTime.Now
                        };

                        Files.Add(newItem);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"导入文件失败 {filePath}: {ex.Message}");
                    }
                }

                if (count > 0)
                {
                    RequestSave();
                    FilesView?.Refresh();
                    MessageBox.Show($"成功导入 {count} 个文件到文件夹 [{targetFolder.Name}]。", "导入完成");
                }
            }
        }

        private async Task InitializeAsync()
        {
            var data = await _dataService.LoadAsync();
            if (data.Folders.Count == 0) data.Folders.Add(new FolderItem { Name = "我的提示词" });
            Folders = new ObservableCollection<FolderItem>(data.Folders);
            Files = new ObservableCollection<PromptItem>(data.Files);
            var fid = Folders.First().Id;
            foreach (var f in Files) if (string.IsNullOrEmpty(f.FolderId)) f.FolderId = fid;
            var v = CollectionViewSource.GetDefaultView(Files);

            if (v != null) v.Filter = FilterFiles;
            FilesView = v;

            Files.CollectionChanged += (s, e) => RequestSave();
            SelectedFolder = Folders.FirstOrDefault();
            IsFullMode = true;
        }

        private bool FilterFiles(object o) => o is PromptItem f && SelectedFolder != null && f.FolderId == SelectedFolder.Id;
        private async void RequestSave() { if (!string.IsNullOrEmpty(Config.UserName)) await SaveDataAsync(); }
        private async Task SaveDataAsync() { try { await _dataService.SaveAsync(Folders, Files); _lastSyncTime = DateTime.Now; UpdateTimeDisplay(); } catch { SyncTimeDisplay = "Err"; } }
        private void UpdateTimeDisplay() { var s = DateTime.Now - _lastSyncTime; SyncTimeDisplay = s.TotalSeconds < 60 ? $"{(int)s.TotalSeconds}s" : $"{(int)s.TotalMinutes}m"; }
    }
}