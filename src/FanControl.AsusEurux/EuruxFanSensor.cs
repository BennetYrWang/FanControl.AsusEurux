using FanControl.Plugins;

namespace FanControl.AsusEurux;

internal sealed class EuruxFanSensor(int port) : IPluginSensor
{
    internal static string GetId(int port) => $"asus-eurux/fan/port-{port + 1}";

    public string Id { get; } = GetId(port);

    public string Name { get; } = $"ROG EURUX Port {port + 1} RPM";

    public float? Value { get; private set; }

    public void Update()
    {
        // EuruxPlugin.Update owns raw polling and calibrated values for all four ports.
    }

    internal void SetValue(float rpm) => Value = rpm;

    internal void Invalidate() => Value = null;
}
