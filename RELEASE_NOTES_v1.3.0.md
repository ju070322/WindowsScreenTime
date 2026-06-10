# Windows Screen Time v1.3.0

## 主要更新

- 新增前台时间、后台时间和总时间统计。
- 新增今日 / 本周视图切换。
- 新增柱状图使用排行。
- 新增 `.wstdata` 数据导出和导入。
- 安装版支持覆盖更新，并保留旧版本数据。
- 安装版默认安装到 `C:\Program Files\WindowsScreenTime`。
- 安装后会显示在 Windows“程序和功能 / 已安装的应用”列表中。
- 新增独立卸载程序 `WindowsScreenTimeUninstall.exe`。

## 界面优化

- 主界面改为 Windows 11 资源管理器风格左侧导航布局。
- 移除顶部多余命令栏，扩大内容显示区域。
- 优化左侧导航和设置按钮显示。
- 修复设置界面底部内容和“取消 / 保存”按钮显示不完整的问题。
- 修复多处中文乱码和布局显示问题。

## 下载

- `WindowsScreenTimeSetup.exe`：安装版，推荐普通用户使用。
- `WindowsScreenTime-Portable.zip`：免安装版，解压后直接运行。

## 数据说明

设置和使用数据保存在：

```text
%LOCALAPPDATA%\WindowsScreenTime
```

安装版更新和默认卸载不会删除这里的统计数据。
