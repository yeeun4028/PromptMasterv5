## 问题定位（已确认冲突点）
- 迷你窗口在发送流程里会立刻隐藏：`TriggerSendProcess()` 第一行就是 `this.Hide()`（[MainWindow.xaml.cs](file:///e:/aDrive_backup/Projects/PromptMasterv5/PromptMasterv5/MainWindow.xaml.cs#L1394-L1407)）。
- 同时，URL 命中检测定时器每 450ms 运行一次；当检测到命中时会 `Show()` + `Topmost=true`（[UpdateMiniUrlMonitor](file:///e:/aDrive_backup/Projects/PromptMasterv5/PromptMasterv5/MainWindow.xaml.cs#L382-L459)）。
- 于是出现你说的时间冲突：按 Enter 触发发送→窗口隐藏→下一次 URL 轮询又把窗口显示回来。

## 修复目标
- 在“按 Enter 发送并隐藏窗口”这一段时间内，以及发送完成后的同一命中页面上，自动显现逻辑不应把窗口拉出来打断发送/输入。
- 仍保留你之前的需求：当用户切换到其它页面再回到命中页面时，迷你窗口仍可自动显现。

## 实现方案（推荐）
### 1) 增加“发送期间/发送后抑制自动显现”状态
- 在 MainWindow 增加字段：
  - `_suppressMiniAutoShowUntilUtc`：发送期间短暂抑制（例如 1500~2500ms）。
  - `_suppressMiniAutoShowUrl`：记录触发发送时的命中 URL（或其关键部分）。
- 在 `TriggerSendProcess()` 开始隐藏前：
  - 读取一次当前地址栏 URL（Chrome/Edge）并记录到 `_suppressMiniAutoShowUrl`。
  - 设置 `_suppressMiniAutoShowUntilUtc = now + 2000ms`。
  - 清掉 `_shouldStealMiniFocusWhenIdle`，避免发送后立刻又抢焦点。

### 2) 在 URL 命中显示逻辑里尊重抑制状态
- 在 `UpdateMiniUrlMonitor()` 获取到 `url` 和 `isMatch` 后：
  - 若 `isMatch==true` 且（当前时间 < `_suppressMiniAutoShowUntilUtc`）→ 不执行 `Show()/Topmost`。
  - 若 `isMatch==true` 且窗口当前是隐藏，并且 `url` 与 `_suppressMiniAutoShowUrl` 相同 → 也不自动 Show（表示“用户刚在这个页面按 Enter 主动隐藏过”）。
  - 当检测到 `isMatch==false`（用户已离开命中页面）时，清空 `_suppressMiniAutoShowUrl`，下一次再回到命中页面就允许自动显现。

### 3) 可选：发送期间暂停 URL 轮询
- 在 `TriggerSendProcess()` 里 `Hide()` 前先 `_miniUrlMonitorTimer.Stop()`，发送结束后再 `Start()`。
- 该项能进一步减少竞态，但如果第 1/2 条完成，通常已经足够稳定；是否启用取决于你是否希望发送期间完全不跑命中检测。

## 需要改动的文件
- [MainWindow.xaml.cs](file:///e:/aDrive_backup/Projects/PromptMasterv5/PromptMasterv5/MainWindow.xaml.cs)
  - `TriggerSendProcess`：设置抑制状态
  - `UpdateMiniUrlMonitor`：增加抑制判断分支

## 验证场景
- 在命中 URL 页面（例如 deepseek）：
  - 按 Enter 触发发送：窗口隐藏且不会马上被“命中检测”拉回来；文字正常发到网页输入框。
  - 发送完成后仍停留在同一命中页面：迷你窗口保持隐藏（直到你切换到非命中页面再切回来）。
  - 切到非命中页面：抑制状态清除；再回到命中页面：迷你窗口再次自动显现。

确认后我就按上述方案修改代码并构建验证。