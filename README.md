# Windows Screen Time

Windows Screen Time 是一个 Windows 桌面屏幕使用时间统计工具。

作者：哈呼呼吗  
项目地址：https://github.com/ju070322/WindowsScreenTime

## 功能

- 统计应用前台时间、后台时间和总时间。
- 支持今日和本周视图切换。
- 柱状图展示应用使用时间排行。
- 关闭窗口时默认最小化到系统托盘。
- 支持开机自启、启动后最小化、空闲判定、采样间隔等设置。
- 支持导出和导入 `.wstdata` 数据文件。
- 安装版支持覆盖更新，并保留旧版本数据。
- 安装版默认安装到 `C:\Program Files\WindowsScreenTime`，并注册到 Windows 程序和功能。
- 安装目录包含独立卸载程序 `WindowsScreenTimeUninstall.exe`。

## 下载

- 安装版：`WindowsScreenTimeSetup.exe`
- 免安装版：`WindowsScreenTime-Portable.zip`

安装版会请求管理员权限，用于写入 `C:\Program Files` 和 Windows 卸载注册表。

## 数据位置

设置和使用数据保存在：

```text
%LOCALAPPDATA%\WindowsScreenTime
```

安装版更新时只替换程序文件，不删除这里的数据。卸载程序默认也会保留这里的统计数据。

## 更新日志

### v1.4.0

- 顶部统计卡片中的“总时间”改为系统开机时间。
- 新增圆环图，用于显示前台时间和后台时间占比。
- 新增柱状图 / 圆环图标签切换。
- 优化统计卡片、图表区和明细区显示比例。
- 修复图表控件中文显示问题。

### v1.3.0

- 新增前台时间、后台时间和总时间统计。
- 新增今日 / 本周视图切换。
- 新增柱状图使用排行。
- 新增 `.wstdata` 数据导出和导入。
- 新增安装版覆盖更新，旧版本数据会保留。
- 新增独立卸载程序 `WindowsScreenTimeUninstall.exe`。
- 安装版改为默认安装到 `C:\Program Files\WindowsScreenTime`。
- 安装版现在会显示在 Windows“程序和功能 / 已安装的应用”列表中。
- 主界面改为 Windows 11 资源管理器风格的左侧导航布局。
- 移除顶部多余命令栏，优化内容区显示空间。
- 修复设置界面底部内容和“取消 / 保存”按钮显示不完整的问题。
- 修复多处中文乱码和布局显示问题。

### v1.2.0

- 新增自定义应用图标。
- 新增安装版和免安装版两种发布形式。
- 新增托盘菜单和关闭最小化到后台。
- 新增设置界面，支持开机自启、启动后最小化、采样间隔、空闲判定等选项。

### v1.1.0

- 新增图形界面。
- 新增本次会话的软件使用时间统计。
- 新增柱状图和应用明细。

### v1.0.0

- 初始版本。
- 支持后台采样当前前台窗口。
- 支持按应用统计使用时间。
- 支持生成 HTML 报告。

## 构建

```powershell
dotnet build WindowsScreenTimeApp\WindowsScreenTimeApp.csproj -c Release
dotnet publish WindowsScreenTimeApp\WindowsScreenTimeApp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

完整发布脚本：

```powershell
powershell -ExecutionPolicy Bypass -File .\build-release.ps1
```

安装器工程在 `WindowsScreenTimeSetupBuilder`，卸载器工程在 `WindowsScreenTimeUninstaller`。
