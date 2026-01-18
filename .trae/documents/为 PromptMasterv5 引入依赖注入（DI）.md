## 我已理解的现状（作为事实来源）
- 已完成 Core/Infrastructure 拆分：模型在 PromptMasterv5.Core.Models；接口在 PromptMasterv5.Core.Interfaces；服务实现迁移到 PromptMasterv5.Infrastructure.Services。
- AiService 实现 IAiService；WebDavDataService 与 FileDataService 实现 IDataService。
- 当前仍存在通过 new Service() 的强耦合，需要进入阶段 3：引入 DI。

## 目标
- 在启动阶段配置 DI 容器。
- 通过容器创建 MainWindow / MainViewModel。
- 用接口注入替换关键的 new Service()（先覆盖最核心路径，后续可渐进扩大）。

## 实施步骤（将要修改的内容）
1. **引入 DI 依赖**
   - 在项目文件中加入 Microsoft.Extensions.DependencyInjection（必要时也加入 Hosting 以支持 IHost 生命周期）。

2. **App.xaml.cs 配置容器并以容器创建主窗口**
   - 在 App 中构建 ServiceProvider（或 IHost），注册接口与实现、ViewModel、MainWindow。
   - OnStartup 中从容器 Resolve MainWindow 并 Show；保留现有全局异常捕获逻辑。
   - OnExit 中 Dispose 容器（若使用 IHost 则 StopAsync/Dispose）。

3. **MainWindow / MainViewModel 构造注入**
   - MainWindow 构造函数接收 MainViewModel（替换内部 new MainViewModel()）。
   - MainViewModel 构造函数接收 IAiService、IDataService 等依赖（根据现有真实调用点决定注入集合）。

4. **替换关键 new Service() 调用（最小闭环）**
   - 扫描并优先替换：AiService、WebDavDataService、FileDataService、BaiduService（若在主流程中被 new）。
   - 对 IDataService 的选择策略：
     - 方案 A（最小改动）：同时注册 WebDavDataService 与 FileDataService，再在 MainViewModel 内依据配置选择使用。
     - 方案 B（更架构化）：引入 IDataServiceFactory / IDataServiceSelector（位于 Core/Interfaces），由 Infrastructure 实现，ViewModel 只依赖工厂。
   - 默认先做方案 A，确保快速打通；后续再演进到方案 B。

5. **验证**
   - 编译通过。
   - 启动应用能正常显示主窗口。
   - 迷你/完整模式切换、基础 AI 调用/数据读写（本地或 WebDAV）至少跑通一条主路径。

## 预期产出
- App.xaml.cs 完成 DI 容器配置。
- MainWindow / MainViewModel 改为构造注入。
- Core 接口与 Infrastructure 实现完成注册。
- 关键 new Service() 强耦合被移除或显著减少。

如果你确认这个方案，我将开始按以上步骤修改 App.xaml.cs 并逐步把 new Service() 迁移到注入方式。