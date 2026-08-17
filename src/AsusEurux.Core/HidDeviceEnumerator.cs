using System.ComponentModel;
using System.Runtime.InteropServices;

namespace AsusEurux;

internal static class HidDeviceEnumerator
{
    internal static IReadOnlyList<string> FindEuruxDevicePaths()
    {
        NativeMethods.HidD_GetHidGuid(out Guid hidGuid);
        IntPtr infoSet = NativeMethods.SetupDiGetClassDevs(
            ref hidGuid,
            null,
            IntPtr.Zero,
            NativeMethods.DigcfPresent | NativeMethods.DigcfDeviceInterface);

        if (infoSet == new IntPtr(-1))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to enumerate HID devices.");
        }

        List<string> paths = [];
        try
        {
            for (uint index = 0; ; index++)
            {
                NativeMethods.SpDeviceInterfaceData interfaceData = new()
                {
                    Size = Marshal.SizeOf<NativeMethods.SpDeviceInterfaceData>()
                };

                if (!NativeMethods.SetupDiEnumDeviceInterfaces(
                        infoSet,
                        IntPtr.Zero,
                        ref hidGuid,
                        index,
                        ref interfaceData))
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == NativeMethods.ErrorNoMoreItems)
                    {
                        break;
                    }

                    throw new Win32Exception(error, "Unable to enumerate a HID device interface.");
                }

                string path = GetDevicePath(infoSet, ref interfaceData);
                if (IsEuruxPath(path))
                {
                    paths.Add(path);
                }
            }
        }
        finally
        {
            NativeMethods.SetupDiDestroyDeviceInfoList(infoSet);
        }

        return paths;
    }

    private static string GetDevicePath(
        IntPtr infoSet,
        ref NativeMethods.SpDeviceInterfaceData interfaceData)
    {
        _ = NativeMethods.SetupDiGetDeviceInterfaceDetail(
            infoSet,
            ref interfaceData,
            IntPtr.Zero,
            0,
            out uint requiredSize,
            IntPtr.Zero);

        int error = Marshal.GetLastWin32Error();
        if (requiredSize == 0 || error != NativeMethods.ErrorInsufficientBuffer)
        {
            throw new Win32Exception(error, "Unable to determine a HID device path length.");
        }

        IntPtr detailData = Marshal.AllocHGlobal(checked((int)requiredSize));
        try
        {
            // SetupAPI expects 8 for Unicode x64 and 6 for Unicode x86. The string starts at byte 4.
            Marshal.WriteInt32(detailData, IntPtr.Size == 8 ? 8 : 6);
            if (!NativeMethods.SetupDiGetDeviceInterfaceDetail(
                    infoSet,
                    ref interfaceData,
                    detailData,
                    requiredSize,
                    out _,
                    IntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to read a HID device path.");
            }

            return Marshal.PtrToStringUni(IntPtr.Add(detailData, sizeof(int)))
                ?? throw new InvalidOperationException("SetupAPI returned an empty HID device path.");
        }
        finally
        {
            Marshal.FreeHGlobal(detailData);
        }
    }

    private static bool IsEuruxPath(string path)
    {
        return path.Contains("vid_0b05&pid_1d98&mi_01", StringComparison.OrdinalIgnoreCase);
    }
}
