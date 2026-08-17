# FanControl.AsusEurux

[简体中文](README.zh-CN.md)

An experimental Windows plugin that lets [FanControl](https://github.com/Rem0o/FanControl.Releases)
read and control the four physical fan ports on the ASUS ROG EURUX GR120 controller.

The controller is identified as `USB VID_0B05 / PID_1D98`. This project talks directly to its
vendor HID interface and does not require the Armoury Crate local HTTP service or ship any
ASUS/ENE proprietary library.

> [!WARNING]
> This is an unofficial, reverse-engineered hardware integration. It has only been tested with
> USB release 0.6. Use conservative minimum fan speeds and monitor temperatures while testing.

## Features

- Four live RPM sensors.
- Four 0–100% FanControl control sensors, automatically paired with their RPM sensors.
- Preserves the other three port targets when one port changes.
- Reasserts active targets on every FanControl update to prevent another ASUS component from
  silently taking control back.
- Restores the PWM snapshot captured at plugin startup when a control is reset or the plugin closes.
- Uses the same global hardware mutex as the installed ENE HAL to reduce concurrent HID access.

Fans daisy-chained to the same EURUX port are controlled as one group; individual fans within a
chain cannot be controlled independently.

## Install a release

1. Download `FanControl.AsusEurux-<version>.zip` from this repository's Releases page.
2. Fully exit FanControl.
3. Extract `FanControl.AsusEurux.dll` and `AsusEurux.Core.dll` into FanControl's `Plugins` folder.
   The installer default is `C:\Program Files (x86)\FanControl\Plugins` and normally requires
   Administrator permission.
4. Restart FanControl. Four `ROG EURUX Port` controls and four RPM sensors should appear.

If Windows blocks a downloaded DLL, open the ZIP file's Properties, select **Unblock**, and extract
it again. FanControl also provides an **Install plugin** button, but manual extraction is recommended
here because this plugin contains two DLLs.

## Build from source

Requirements: Windows 10/11 and the .NET 10 SDK.

If FanControl is installed in its default location:

```powershell
dotnet build .\AsusFanControlBridge.slnx -c Release
dotnet run --project .\tests\AsusEurux.ProtocolTests -c Release
```

For a reproducible build without a local FanControl installation, fetch the pinned public plugin
API first:

```powershell
.\scripts\restore-fancontrol-sdk.ps1
dotnet build .\AsusFanControlBridge.slnx -c Release `
  -p:FanControlDir="$PWD\.deps\FanControl"
```

The hardware smoke test requires a connected EURUX controller and is read-only:

```powershell
dotnet run --project .\tests\AsusEurux.PluginSmokeTests -c Release `
  -p:FanControlDir="$PWD\.deps\FanControl"
```

To build and install from source, fully exit FanControl and run an elevated PowerShell:

```powershell
.\scripts\install.ps1
```

## Diagnostic probe

Read-only status:

```powershell
dotnet run --project .\src\AsusEurux.Probe -- status
```

Explicit hardware write, intended only for diagnostics:

```powershell
dotnet run --project .\src\AsusEurux.Probe -- set 1 50 --allow-write
```

The write command does not restore the previous value when it exits. Prefer the FanControl plugin
for normal use.

## Safety and known limitations

- Do not keep Armoury Crate's fan-control page active while this plugin controls the same device.
- Initialization must read all four PWM values successfully before the plugin permits writes.
- A target of `0` is sent unchanged, but it is not yet known whether every firmware interprets it as
  fan stop or "do not update this port". Configure a minimum above zero.
- Restore means the PWM snapshot read at plugin startup; it does not re-enable an Armoury Crate
  temperature curve.
- The controller may continue reporting its previous configured PWM after a successful write.
  Verify behavior using RPM, not the returned PWM value.

## Protocol notes

The HID report ID is `0x90`; feature reports are 32 bytes and input reports are 65 bytes.

| Operation | Feature report prefix | Response |
| --- | --- | --- |
| Query PWM | `90 05 00` | `90 F0 90 p1 p2 p3 p4 ...` |
| Query RPM | `90 05 01` | `90 F1 rpm1_be rpm2_be rpm3_be rpm4_be ...` |
| Set PWM | `90 04 p1 p2 p3 p4` | No input response |

The protocol was documented through observation and analysis of ENE Fan HAL 1.0.18.1 behavior.
No ASUS/ENE binary or source code is included or redistributed.

## Project status

This is early hardware-specific software. The initial implementation has been validated on one
controller: raising port 1 from 54% to 70% increased its fan speed from roughly 1290 RPM to
1590–1620 RPM while port 2 remained unchanged; restoring 54% returned port 1 to roughly 1290 RPM.

Contributions and additional firmware reports are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md).

## License and trademarks

Source code in this repository is licensed under the [MIT License](LICENSE).

ASUS, ROG, EURUX, Armoury Crate, and related marks belong to their respective owners. FanControl is
a separate project. This repository is not affiliated with or endorsed by ASUS, ENE, or FanControl.
