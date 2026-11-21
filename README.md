# IndustrialMonitor · 工业设备数据实时监控看板

面向中小型工业现场的桌面监控台：.NET 8 WPF + LiveCharts2 双通道曲线，
登录认证与三级权限（RBAC）、报警防抖引擎、SQLite 持久化、配置热重载。
需求来源与逐条对照见 `docs/需求文档.md`。

## 功能总览

| 模块 | 能力 |
|------|------|
| 登录认证 | SHA256 哈希 + SQLite 用户库；三级角色 Admin / Operator / Observer |
| 仪表盘 | 设备总数 / 在线数 / 今日报警 / 在线率四卡片，2 秒自动刷新 |
| 实时监控 | TabControl 切设备；温度/压力双轴折线图 + 虚线阈值线；报警灯呼吸闪烁；最近报警列表 |
| 报警中心 | 类型筛选、彩色标签、上下文化报警内容、分页 |
| 设备管理 | 绿/红状态圆点、增删改查、JSON 台账持久化 |
| 历史记录 | 日期 + 设备筛选、分页查询 |
| 系统设置 | 监控参数保存即热生效（FileSystemWatcher）、修改密码 |
| 用户管理 | 管理员专属；新增 / 启用 / 禁用用户 |
| 权限控制 | 菜单按角色过滤 + 页面内操作按钮隐藏（按钮级 RBAC） |

## 目录结构

```
E:\wpf-monitor\
├─ IndustrialMonitor.slnx     解决方案（唯一入口）
├─ .editorconfig              代码风格（解决方案级）
├─ Directory.Build.props      全解决方案共享元数据：版本单点、Nullable、LangVersion
├─ Directory.Packages.props   中央包管理（CPM）：所有 PackageReference 的版本在此
├─ nuget.config               包源配置
├─ .gitignore
├─ README.md                  本文件：使用说明
├─ AGENTS.md                  组织规范（东西放在哪里、为什么）
├─ docs/                      需求文档等
└─ src/IndustrialMonitor/     WPF 主程序（Models / Services / ViewModels / Views / Converters）
```

## 构建与运行

```bash
dotnet build IndustrialMonitor.slnx -c Release   # 0 警告 0 错误
dotnet run --project src/IndustrialMonitor
```

首次启动自动创建 `users.db`（用户库）与 `devices.json`（设备台账）。

**默认账号**：

| 账号 | 密码 | 角色 |
|---|---|---|
| admin | admin123 | 管理员（全部功能） |
| operator | 123456 | 操作员（除用户管理外） |
| observer | 123456 | 观察者（仅总览/监控/报警，只读） |

## 自检（Smoke）

```bash
dotnet run --project src/IndustrialMonitor -- --smoke
```

无人工干预跑完：登录 → 逐页导航 → 数据/图表校验 → 报警引擎端到端（降阈值 → 热重载 → 3 秒防抖 → 落库）→
权限菜单过滤 → 截图输出到 `shots/` → 结果写 `smoke.log`（全部 PASS 才算通过）。

## 约定

- 提交信息用中文 Conventional Commits，尾注版本号（如 `chore(repo): … (1.0.0)`）；
- 包版本只在 `Directory.Packages.props` 维护，程序集版本只在 `Directory.Build.props` 维护；
- `dotnet build IndustrialMonitor.slnx` 0 警告 0 错误，改动后跑一次 `--smoke` 自检。
