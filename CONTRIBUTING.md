# Contributing

Thanks for helping improve support for the ROG EURUX controller.

## Before opening an issue

- Use the latest published plugin and FanControl .NET 10 release.
- Fully close Armoury Crate's fan-control page before reproducing a control problem.
- Run the read-only probe and record all four PWM/RPM values.
- Remove USB serial numbers, account names, and unrelated machine information from logs.
- Never upload ASUS/ENE proprietary DLLs to this repository.

Include the controller USB release/firmware version, FanControl version, plugin version, Windows
version, expected behavior, and actual behavior. State clearly whether a report is read-only or
involves PWM writes.

## Development

Use Windows 10/11 and the .NET 10 SDK. Fetch the pinned FanControl plugin API and build:

```powershell
.\scripts\restore-fancontrol-sdk.ps1
dotnet build .\AsusFanControlBridge.slnx -c Release `
  -p:FanControlDir="$PWD\.deps\FanControl"
```

Local protocol and hardware smoke tests are intentionally kept outside the published repository.
Do not add an automated CI test that writes fan duty. Any manual write test must start from the
current duty, avoid reducing cooling, and restore the original value in a `finally` block.

Keep protocol changes isolated in `AsusEurux.Core`, add parser/report tests, and document the
evidence for new commands without copying proprietary implementation code.
