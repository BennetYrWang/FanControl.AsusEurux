using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace AsusEurux;

/// <summary>Direct HID access to one ROG EURUX controller.</summary>
public sealed class EuruxDevice : IDisposable
{
    private const string HardwareMutexName = @"Global\ENE_WINUSB_MUTEX";
    private static readonly TimeSpan HardwareMutexTimeout = TimeSpan.FromSeconds(2);

    private readonly object _gate = new();
    private readonly SafeFileHandle _handle;
    private readonly Mutex _hardwareMutex;
    private bool _disposed;

    private EuruxDevice(string devicePath, SafeFileHandle handle)
    {
        DevicePath = devicePath;
        _handle = handle;
        _hardwareMutex = new Mutex(false, HardwareMutexName);
    }

    public string DevicePath { get; }

    public static IReadOnlyList<string> FindDevicePaths() => HidDeviceEnumerator.FindEuruxDevicePaths();

    public static EuruxDevice OpenFirst()
    {
        string? path = FindDevicePaths().FirstOrDefault();
        if (path is null)
        {
            throw new InvalidOperationException(
                "ROG EURUX Controller (USB VID_0B05/PID_1D98, HID MI_01) was not found.");
        }

        SafeFileHandle handle = NativeMethods.CreateFile(
            path,
            NativeMethods.GenericRead | NativeMethods.GenericWrite,
            NativeMethods.FileShareRead | NativeMethods.FileShareWrite,
            IntPtr.Zero,
            NativeMethods.OpenExisting,
            0,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();
            handle.Dispose();
            throw new Win32Exception(
                error,
                "Unable to open the ROG EURUX HID interface. Close Armoury Crate fan control and retry.");
        }

        return new EuruxDevice(path, handle);
    }

    public byte[] ReadDuties()
    {
        byte[] response = Query(EuruxProtocol.CreateDutyQuery());
        return EuruxProtocol.ParseDuties(response);
    }

    public ushort[] ReadRpms()
    {
        byte[] response = Query(EuruxProtocol.CreateRpmQuery());
        return EuruxProtocol.ParseRpms(response);
    }

    public void WriteDuties(ReadOnlySpan<byte> duties)
    {
        byte[] report = EuruxProtocol.CreateSetDutyReport(duties);
        ExecuteLocked(() => SetFeature(report));
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _handle.Dispose();
            _hardwareMutex.Dispose();
        }
    }

    private byte[] Query(byte[] request)
    {
        return ExecuteLocked(() =>
        {
            SetFeature(request);

            byte[] response = new byte[EuruxProtocol.InputReportLength];
            response[0] = EuruxProtocol.ReportId;
            if (!NativeMethods.HidD_GetInputReport(_handle, response, response.Length))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "The ROG EURUX controller did not return an input report.");
            }

            return response;
        });
    }

    private void SetFeature(byte[] report)
    {
        if (!NativeMethods.HidD_SetFeature(_handle, report, report.Length))
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "The ROG EURUX controller rejected a feature report.");
        }
    }

    private T ExecuteLocked<T>(Func<T> action)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            bool acquired = false;
            try
            {
                try
                {
                    acquired = _hardwareMutex.WaitOne(HardwareMutexTimeout);
                }
                catch (AbandonedMutexException)
                {
                    acquired = true;
                }

                if (!acquired)
                {
                    throw new TimeoutException(
                        "Timed out waiting for the ENE fan-controller hardware mutex. " +
                        "Armoury Crate may be using the EURUX controller.");
                }

                return action();
            }
            finally
            {
                if (acquired)
                {
                    _hardwareMutex.ReleaseMutex();
                }
            }
        }
    }

    private void ExecuteLocked(Action action)
    {
        _ = ExecuteLocked(() =>
        {
            action();
            return true;
        });
    }
}
