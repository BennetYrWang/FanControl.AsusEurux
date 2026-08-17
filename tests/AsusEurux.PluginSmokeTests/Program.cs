using FanControl.AsusEurux;
using FanControl.Plugins;

TestLogger logger = new();
TestSensorsContainer sensors = new();
EuruxPlugin plugin = new(logger);

try
{
    plugin.Initialize();
    plugin.Load(sensors);

    AssertEqual("fan sensor count", 4, sensors.FanSensors.Count);
    AssertEqual("control sensor count", 4, sensors.ControlSensors.Count);

    plugin.Update();

    if (sensors.FanSensors.Any(sensor => sensor.Value is null))
    {
        throw new InvalidOperationException("At least one RPM sensor did not receive a value.");
    }

    Console.WriteLine(
        $"Plugin smoke test passed. RPM: {string.Join(", ", sensors.FanSensors.Select(sensor => sensor.Value))}.");
}
finally
{
    // No control sensor is touched by this test, so Close remains read-only.
    plugin.Close();
}

static void AssertEqual<T>(string name, T expected, T actual)
    where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected {expected}, got {actual}.");
    }
}

internal sealed class TestLogger : IPluginLogger
{
    public void Log(string message) => Console.WriteLine(message);
}

internal sealed class TestSensorsContainer : IPluginSensorsContainer
{
    public List<IPluginControlSensor> ControlSensors { get; } = [];

    public List<IPluginSensor> FanSensors { get; } = [];

    public List<IPluginSensor> TempSensors { get; } = [];
}
