# AGENTS.md · 仓库组织规范

> 本文件是**项目结构与文件归属的唯一规范**，面向在本仓库工作的开发者与 AI 代理。
> 新增文件、调整解决方案分层前先读本文件；与 README（面向使用者）分工不同：
> README 讲"怎么构建与使用"，本文件讲"东西放在哪里、为什么"。

---

## 1. 顶层布局

```
E:\wpf-monitor\
├─ IndustrialMonitor.slnx     解决方案（唯一入口）
├─ .editorconfig              代码风格（解决方案级）
├─ Directory.Build.props      全解决方案共享元数据：版本单点、Deterministic、语言基线
├─ Directory.Packages.props   中央包管理（CPM）：所有 PackageReference 的版本在此
├─ nuget.config              包源配置
├─ .gitignore
├─ README.md                  使用说明（构建/运行/账号/自检）
├─ AGENTS.md                  本文件：组织规范
├─ docs/                      需求文档、验收材料
└─ src/                       源码项目（可交付产物）
```

**Solution Items = 解决方案级文件**（`.editorconfig`、`README.md`、`AGENTS.md`、
`Directory.Build.props` / `Directory.Packages.props`、`nuget.config`、`.gitignore`），
必须留在仓库根目录——MSBuild 的 `Directory.Build.props` / `Directory.Packages.props`
只在目录树向上查找时才生效，挪走即失效。

---

## 2. 源码项目

### 2.1 `src/IndustrialMonitor` — WPF 主程序

| 目标框架 | 职责 | 内部分层 |
|---|---|---|
| `net8.0-windows` (WinExe) | WPF 主程序、DI 组装、页面与导航 | `Models/`（数据契约）、`Services/`（认证/存储/模拟数据/配置）、`ViewModels/`（状态编排）、`Views/`（页面与窗口）、`Converters/`（值转换）、`Themes/`（主题资源） |

归属判定顺序（新代码放哪里）：

1. 纯数据契约 → `Models/`；
2. 与持久化 / 设备 / 模拟数据 / 认证相关 → `Services/`（接口与实现同目录成对，`I*.cs` + `*.cs`）；
3. 只服务某个页面的状态编排 → `ViewModels/`；
4. 只做 UI 值转换 → `Converters/`；
5. 跨页面共享的视觉资源 → `Themes/AppTheme.xaml`。

### 2.2 运行时自检

`SmokeTest.cs` 由 `App.xaml.cs` 通过 `--smoke` 参数启用，随主程序发布但不参与正常启动路径；
后续如扩充为独立测试工程，放 `tests/IndustrialMonitor.App.Tests`，命名沿用
`<产品>.<被测层>.Tests`，且测试工程不得被 `src/` 引用。

---

## 3. 非代码目录

| 目录 | 放什么 | 不放什么 |
|---|---|---|
| `docs/` | 需求文档（`docs/需求文档.md`）、实施与验收材料 | 代码、构建产物 |
| `src/IndustrialMonitor/Themes/` | XAML 主题资源字典 | 业务逻辑 |

运行时产物（`users.db`、`devices.json`、`shots/`、`*.log`）一律不入 Git（见 `.gitignore`）。

---

## 4. 新增文件/项目的检查清单

- [ ] 位置符合 §2 的归属规则，命名符合 `IndustrialMonitor.<层>`；
- [ ] 依赖方向：`Views → ViewModels → Services → Models`，无反向引用；
- [ ] `IndustrialMonitor.slnx` 已加入对应分组（新增项目必须同时改磁盘目录 + slnx）；
- [ ] `dotnet build IndustrialMonitor.slnx -c Release` 0 警告 0 错误、`--smoke` 自检全 PASS；
- [ ] 文档与代码同批提交，版本号只改 `Directory.Build.props` 一处；
- [ ] 提交信息用中文 Conventional Commits，尾注版本号（如 `feat(monitor): … (1.0.0)`）。
