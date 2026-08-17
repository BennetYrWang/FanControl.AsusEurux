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
        // EuruxPlugin.Update performs one transaction for all four RPM values.
    }

    internal void SetValue(ushort rpm) => Value = rpm;

    internal void Invalidate() => Value = null;
}
