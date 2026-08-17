# Publishing checklist

This file is for maintainers preparing a public release.

1. Build on Windows with the pinned FanControl .NET 10 plugin API.
2. Run the protocol tests and the read-only hardware smoke test.
3. Perform any PWM write validation conservatively and restore the starting duty in `finally`.
4. Update `VersionPrefix`, release notes, and the tested firmware list.
5. Commit the release changes and push a signed or annotated `v<semver>` tag.
6. Confirm that the Release workflow publishes the ZIP and SHA-256 checksum.
7. Test the ZIP in a clean FanControl installation by extracting its complete contents into
   `Plugins` and restarting FanControl.
8. Announce the plugin in the FanControl repository's **Show and tell** discussions and ask the
   maintainer to add it to the README's **From the Community** list.

Do not attach or redistribute ASUS/ENE binaries, private symbols, or logs containing device serial
numbers. The FanControl plugin API is downloaded only as a temporary build dependency and is not
included in this repository's release package.
