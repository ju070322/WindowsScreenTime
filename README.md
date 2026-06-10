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

## 数据位置

设置和使用数据保存在：

```text
%LOCALAPPDATA%\WindowsScreenTime
```

安装版更新时只替换程序文件，不删除这里的数据。

## 构建

```powershell
dotnet build WindowsScreenTimeApp\WindowsScreenTimeApp.csproj -c Release
dotnet publish WindowsScreenTimeApp\WindowsScreenTimeApp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

安装器工程在 `WindowsScreenTimeSetupBuilder`。
