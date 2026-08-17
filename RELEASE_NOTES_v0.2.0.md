# FanControl.AsusEurux v0.2.0

## Highlights

- Updated PWM control for USB firmware 0.6: FanControl's logical 0% is translated to protocol duty 1 because protocol duty 0 is a special value and does not stop the fan.
- Controlled ports now report a calibrated RPM estimate based on the requested PWM duty instead of the controller's stale raw RPM response.
- Uncontrolled ports continue to report the raw RPM value returned by the controller.

## RPM estimate

- Below 5% duty: 0 RPM.
- At 5% duty: 300 RPM.
- At 100% duty: 2250 RPM.
- Values between 5% and 100% are linearly interpolated.

## Known limitations

- The estimated RPM cannot detect a stalled or disconnected fan while that port is controlled.
- The estimate is calibrated against the tested USB firmware 0.6 behavior and may not match other firmware or fan models.
- Do not let Armoury Crate's fan-control page control the same device at the same time.
