# FanControl.AsusEurux

[English](README.md)

这是一个面向 Windows 版 [FanControl](https://github.com/Rem0o/FanControl.Releases) 的实验性插件，
让 FanControl 直接读取和控制 ASUS ROG EURUX GR120 控制器的四个物理风扇端口。

控制器枚举为 `USB VID_0B05 / PID_1D98`。插件运行时使用 Windows 标准 HID 与 SetupAPI
接口直接访问控制器，不加载或重新分发厂商专有程序库，也不依赖 Armoury Crate 的本机 HTTP
服务。

> [!WARNING]
> 这是非官方的实验性硬件集成，目前只在 USB release 0.6 上测试过。测试时请设置保守的最低
> 转速并持续监控温度。

## 功能

- 读取四个端口的实时 RPM。
- 在 FanControl 中提供四个 0–100% 控制项，并与 RPM 传感器自动配对。
- 修改一路时保留其余三路的目标值。
- 控制生效期间，每次 FanControl 更新都会重申目标 PWM，避免 ASUS 后台组件静默夺回控制权。
- 禁用控制或关闭插件时，恢复插件启动时读取到的 PWM 快照。
- 使用全局命名互斥锁串行访问控制器，减少同时访问 HID 的冲突。

同一个 EURUX 端口下菊花链连接的多把风扇只能作为一组调速，不能逐把控制。

## 安装发布版

1. 从本仓库 Releases 页面下载 `FanControl.AsusEurux-<version>.zip`。
2. 完全退出 FanControl。
3. 把 `FanControl.AsusEurux.dll` 和 `AsusEurux.Core.dll` 解压到 FanControl 的 `Plugins` 目录。
   安装器默认路径为 `C:\Program Files (x86)\FanControl\Plugins`，通常需要管理员权限。
4. 重启 FanControl，应出现四个 `ROG EURUX Port` 控制项和四个 RPM 传感器。

如果 Windows 阻止下载的 DLL，请打开 ZIP 属性、选择“解除锁定”，再重新解压。FanControl 也有
“Install plugin”按钮，但本插件包含两个 DLL，建议手动解压整个发布包。

## 从源码构建

要求 Windows 10/11 和 .NET 10 SDK。FanControl 安装在默认位置时：

```powershell
dotnet build .\AsusFanControlBridge.slnx -c Release
dotnet run --project .\tests\AsusEurux.ProtocolTests -c Release
```

没有安装 FanControl 时，可先获取 CI 固定版本的公开插件接口：

```powershell
.\scripts\restore-fancontrol-sdk.ps1
dotnet build .\AsusFanControlBridge.slnx -c Release `
  -p:FanControlDir="$PWD\.deps\FanControl"
```

硬件冒烟测试需要连接 EURUX 控制器，但测试本身只读：

```powershell
dotnet run --project .\tests\AsusEurux.PluginSmokeTests -c Release `
  -p:FanControlDir="$PWD\.deps\FanControl"
```

从源码安装时，请完全退出 FanControl，然后从管理员 PowerShell 运行：

```powershell
.\scripts\install.ps1
```

## 诊断探针

只读查询：

```powershell
dotnet run --project .\src\AsusEurux.Probe -- status
```

显式写入，仅用于排障：

```powershell
dotnet run --project .\src\AsusEurux.Probe -- set 1 50 --allow-write
```

写入命令退出时不会自动恢复原值，日常使用请通过 FanControl 插件控制。

## 安全与已知限制

- 不建议让 Armoury Crate 风扇控制页面与本插件同时控制同一设备。
- 初始化必须成功读取四路 PWM，插件才允许写入。
- `0` 会原样发送，但目前不能确认所有固件把它解释为停转还是“不更新此端口”；请设置大于 0
  的最低百分比。
- “恢复”指恢复插件启动时的 PWM 快照，不是重新启用 Armoury Crate 温控曲线。
- 控制器在写入成功后仍可能返回旧 PWM 配置值；应通过 RPM 变化确认是否生效。

## 项目状态

这是早期的硬件专用项目。初版已在一台控制器上验证：1 号端口从 54% 提升至 70% 后，转速由
约 1290 RPM 上升至 1590–1620 RPM，2 号端口保持不变；恢复 54% 后，1 号端口回到约
1290 RPM。

欢迎贡献代码和其他固件版本的测试结果，参见 [CONTRIBUTING.md](CONTRIBUTING.md)。

## 许可证与商标

本仓库源码采用 [MIT License](LICENSE)。ASUS、ROG、EURUX、Armoury Crate 等商标归其
权利人所有；FanControl 是独立项目。本仓库与 ASUS 或 FanControl 均无隶属或背书关系。
