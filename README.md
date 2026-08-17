# FanControl.AsusEurux

[简体中文](README.zh-CN.md)

An experimental Windows plugin that lets [FanControl](https://github.com/Rem0o/FanControl.Releases)
read and control the four physical fan ports on the ASUS ROG EURUX GR120 controller.

The controller is identified as `USB VID_0B05 / PID_1D98`. At runtime, the plugin uses the standard
Windows HID and SetupAPI interfaces to communicate directly with the controller. It does not load
or redistribute proprietary controller libraries and does not depend on the Armoury Crate local
HTTP service.

> [!WARNING]
> This is an unofficial, experimental hardware integration. It has only been tested with
> USB release 0.6. Use conservative minimum fan speeds and monitor temperatures while testing.

## Features

- Four RPM sensors. Ports controlled by FanControl use a calibrated estimate based on the requested
  duty; uncontrolled ports continue to show the controller's raw RPM reading.
- Four 0–100% FanControl control sensors, automatically paired with their RPM sensors.
- Maps FanControl's logical 0% to protocol duty 1 because firmware 0.6 does not interpret protocol
  duty 0 as fan stop.
- Preserves the other three port targets when one port changes.
- Reasserts active targets on every FanControl update to prevent another ASUS component from
  silently taking control back.
- Restores the PWM snapshot captured at plugin startup when a control is reset or the plugin closes.
- Serializes controller access with a global named mutex to reduce concurrent HID access.

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
```

For a reproducible build without a local FanControl installation, fetch the pinned public plugin
API first:

```powershell
.\scripts\restore-fancontrol-sdk.ps1
dotnet build .\AsusFanControlBridge.slnx -c Release `
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
- On tested USB firmware 0.6, protocol duty `0` is a special value and does not stop the fan.
  FanControl 0% is therefore sent as protocol duty `1`; the raw diagnostic probe remains unchanged.
- While a port is controlled, its displayed RPM is an estimate: duties below 5% show 0 RPM, 5%
  shows 300 RPM, and 100% shows 2250 RPM with linear interpolation in between. This estimate cannot
  detect a stalled or disconnected fan.
- Restore means the PWM snapshot read at plugin startup; it does not re-enable an Armoury Crate
  temperature curve.
- The controller may continue reporting its previous configured PWM after a successful write.
  Verify behavior using RPM, not the returned PWM value.

## Project status

This is early hardware-specific software. The initial implementation has been validated on one
controller: raising port 1 from 54% to 70% increased its fan speed from roughly 1290 RPM to
1590–1620 RPM while port 2 remained unchanged; restoring 54% returned port 1 to roughly 1290 RPM.

Contributions and additional firmware reports are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md).

## License and trademarks

Source code in this repository is licensed under the [MIT License](LICENSE).

ASUS, ROG, EURUX, Armoury Crate, and related marks belong to their respective owners. FanControl is
a separate project. This repository is not affiliated with or endorsed by ASUS or FanControl.
