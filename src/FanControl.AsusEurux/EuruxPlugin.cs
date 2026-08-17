using AsusEurux;
using FanControl.Plugins;

namespace FanControl.AsusEurux;

public sealed class EuruxPlugin : IPlugin2
{
    private readonly object _gate = new();
    private readonly IPluginLogger? _logger;
    private readonly EuruxFanSensor[] _fanSensors =
        Enumerable.Range(0, EuruxProtocol.PortCount).Select(port => new EuruxFanSensor(port)).ToArray();
    private readonly bool[] _touched = new bool[EuruxProtocol.PortCount];

    private EuruxDevice? _device;
    private byte[]? _originalDuties;
    private byte[]? _currentDuties;
    private int _consecutiveUpdateErrors;

    public EuruxPlugin()
    {
    }

    public EuruxPlugin(IPluginLogger logger)
    {
        _logger = logger;
    }

    public string Name => "ASUS ROG EURUX";

    public void Initialize()
    {
        lock (_gate)
        {
            CloseCore(restore: true);

            try
            {
                _device = EuruxDevice.OpenFirst();
                _originalDuties = _device.ReadDuties();
                _currentDuties = (byte[])_originalDuties.Clone();
                ushort[] rpms = _device.ReadRpms();
                UpdateFanSensors(rpms);
                Log($"Opened {_device.DevicePath}. Initial PWM: {string.Join(", ", _originalDuties)}%.");
            }
            catch (Exception exception)
            {
                Log($"Initialization failed: {exception}");
                CloseCore(restore: false);
            }
        }
    }

    public void Load(IPluginSensorsContainer container)
    {
        lock (_gate)
        {
            if (_device is null)
            {
                return;
            }

            container.FanSensors.AddRange(_fanSensors);
            container.ControlSensors.AddRange(
                Enumerable.Range(0, EuruxProtocol.PortCount)
                    .Select(port => (IPluginControlSensor)new EuruxControlSensor(this, port)));
        }
    }

    public void Update()
    {
        lock (_gate)
        {
            if (_device is null)
            {
                return;
            }

            try
            {
                // ASUS' duty query can keep returning the previous configured value after a
                // successful write. Reassert active FanControl targets once per plugin update
                // so another ASUS component cannot silently take control back.
                if (_touched.Any(value => value))
                {
                    _device.WriteDuties(_currentDuties!);
                }

                UpdateFanSensors(_device.ReadRpms());
                _consecutiveUpdateErrors = 0;
            }
            catch (Exception exception)
            {
                foreach (EuruxFanSensor sensor in _fanSensors)
                {
                    sensor.Invalidate();
                }

                _consecutiveUpdateErrors++;
                if (_consecutiveUpdateErrors <= 3 || _consecutiveUpdateErrors % 60 == 0)
                {
                    Log($"Hardware update failed ({_consecutiveUpdateErrors} consecutive failures): {exception.Message}");
                }
            }
        }
    }

    public void Close()
    {
        lock (_gate)
        {
            CloseCore(restore: true);
        }
    }

    internal void SetDuty(int port, byte duty)
    {
        lock (_gate)
        {
            EnsureReady(port);
            if (_currentDuties![port] == duty && _touched[port])
            {
                return;
            }

            byte previous = _currentDuties[port];
            _currentDuties[port] = duty;
            try
            {
                _device!.WriteDuties(_currentDuties);
                _touched[port] = true;
            }
            catch
            {
                _currentDuties[port] = previous;
                throw;
            }
        }
    }

    internal void ResetDuty(int port)
    {
        lock (_gate)
        {
            EnsureReady(port);
            if (!_touched[port])
            {
                return;
            }

            byte previous = _currentDuties![port];
            _currentDuties[port] = _originalDuties![port];
            try
            {
                _device!.WriteDuties(_currentDuties);
                _touched[port] = false;
            }
            catch
            {
                _currentDuties[port] = previous;
                throw;
            }
        }
    }

    private void CloseCore(bool restore)
    {
        if (_device is not null && restore && _originalDuties is not null && _touched.Any(value => value))
        {
            try
            {
                _device.WriteDuties(_originalDuties);
                Log($"Restored startup PWM values: {string.Join(", ", _originalDuties)}%.");
            }
            catch (Exception exception)
            {
                Log($"Failed to restore startup PWM values: {exception}");
            }
        }

        _device?.Dispose();
        _device = null;
        _originalDuties = null;
        _currentDuties = null;
        Array.Clear(_touched);
        _consecutiveUpdateErrors = 0;

        foreach (EuruxFanSensor sensor in _fanSensors)
        {
            sensor.Invalidate();
        }
    }

    private void EnsureReady(int port)
    {
        if (port is < 0 or >= EuruxProtocol.PortCount)
        {
            throw new ArgumentOutOfRangeException(nameof(port));
        }

        if (_device is null || _originalDuties is null || _currentDuties is null)
        {
            throw new InvalidOperationException("The ROG EURUX controller is not initialized.");
        }
    }

    private void UpdateFanSensors(IReadOnlyList<ushort> rpms)
    {
        for (int port = 0; port < EuruxProtocol.PortCount; port++)
        {
            _fanSensors[port].SetValue(rpms[port]);
        }
    }

    private void Log(string message) => _logger?.Log($"[ASUS ROG EURUX] {message}");
}
