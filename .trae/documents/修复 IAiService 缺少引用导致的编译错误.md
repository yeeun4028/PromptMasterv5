# 修复编译错误计划

根据分析，`AiService.cs` 中已经正确实现了所有接口方法，错误的主要原因是 `IAiService.cs` 缺少了必要的引用，导致编译器无法识别 `ChatMessage` 类型和 `List<>` 泛型，进而引发连锁反应报告“未实现接口”的错误。

## 修改计划

### 1. 修复 IAiService.cs 引用缺失
在 `e:\aDrive_backup\Projects\PromptMasterv5\PromptMasterv5\Core\Interfaces\IAiService.cs` 文件头部添加以下引用：
```csharp
using System.Collections.Generic;
using OpenAI.ObjectModels.RequestModels;
```

这应该能同时解决 `CS0246` (找不到类型) 和 `CS0535` (未实现接口) 错误。