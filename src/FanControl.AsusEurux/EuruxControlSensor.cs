using FanControl.Plugins;

namespace FanControl.AsusEurux;

internal sealed class EuruxControlSensor(EuruxPlugin plugin, int port) : IPluginControlSensor2
{
    public string Id { get; } = $"asus-eurux/control/port-{port + 1}";

    public string Name { get; } = $"ROG EURUX Port {port + 1}";

    public string PairedFanSensorId { get; } = EuruxFanSensor.GetId(port);

    public float? Value { get; private set; }

    public void Set(float value)
    {
        byte duty = checked((byte)Math.Clamp((int)MathF.Round(value), 0, 100));
        plugin.SetDuty(port, duty);
        Value = duty;
    }

    public void Reset()
    {
        plugin.ResetDuty(port);
        Value = null;
    }

    public void Update()
    {
        // Set and Reset own the control state; no separate polling is needed.
    }
}
