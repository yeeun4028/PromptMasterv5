# 计划：优化底部提示词显示方式设置

## 1. 创建反向布尔值转换器
为了实现“图标”和“名称”开关的互斥联动（一个开启时另一个自动关闭），需要创建一个转换器。
- **新建文件**: `Converters/InverseBooleanConverter.cs`
- **功能**: 实现 `IValueConverter`，在 `Convert` 和 `ConvertBack` 中均返回 `!value`。

## 2. 升级配置属性为可观察对象
为了让两个开关的状态在界面上实时同步（例如点击“名称”开关时，“图标”开关自动关闭），需要修改配置类。
- **修改文件**: `Core/Models/LocalSettings.cs`
- **操作**: 将 `MiniPinnedPromptShowIcons` 属性重构为 `[ObservableProperty]` 自动生成的属性，确保引发属性更改通知。

## 3. 更新设置页面 UI
根据您的要求重构界面布局。
- **修改文件**: `Views/SettingsView.xaml`
- **资源**: 在 `UserControl.Resources` 中引入 `InverseBooleanConverter`。
- **布局调整**:
    1.  将标题从 **“迷你窗口底部显示提示词方式”** 修改为 **“底部显示提示词方式”**。
    2.  移除标题右侧的旧开关。
    3.  在标题下方新增一个 `Grid` 或 `StackPanel`，包含两行配置：
        -   **第一行**: 左侧文本 **“图标”**，右侧滑动开关（绑定 `MiniPinnedPromptShowIcons`）。
        -   **第二行**: 左侧文本 **“名称”**，右侧滑动开关（绑定 `MiniPinnedPromptShowIcons`，使用 `InverseBooleanConverter`）。

## 4. 验证
- 编译运行。
- 进入设置 -> 极简窗口设置页。
- 确认标题已更新。
- 点击“图标”开关，确认“名称”开关自动关闭，且界面底部显示图标。
- 点击“名称”开关，确认“图标”开关自动关闭，且界面底部显示名称。
