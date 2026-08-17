using AsusEurux;

return Run(args);

static int Run(string[] args)
{
    try
    {
        using EuruxDevice device = EuruxDevice.OpenFirst();
        Console.WriteLine($"Device: {device.DevicePath}");

        if (args.Length == 0 || string.Equals(args[0], "status", StringComparison.OrdinalIgnoreCase))
        {
            PrintStatus(device);
            return 0;
        }

        if (args.Length == 4 &&
            string.Equals(args[0], "set", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(args[3], "--allow-write", StringComparison.OrdinalIgnoreCase))
        {
            int port = int.Parse(args[1]);
            byte duty = byte.Parse(args[2]);
            if (port is < 1 or > EuruxProtocol.PortCount || duty > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(args),
                    "Port must be 1-4 and duty must be 0-100.");
            }

            byte[] duties = device.ReadDuties();
            Console.WriteLine($"Before: {string.Join("%, ", duties)}%");
            duties[port - 1] = duty;
            device.WriteDuties(duties);
            Thread.Sleep(250);
            PrintStatus(device);
            return 0;
        }

        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  AsusEurux.Probe status");
        Console.Error.WriteLine("  AsusEurux.Probe set <port 1-4> <duty 0-100> --allow-write");
        return 1;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"ERROR: {exception.Message}");
        return 2;
    }
}
static void PrintStatus(EuruxDevice device)
{
    byte[] duties = device.ReadDuties();
    ushort[] rpms = device.ReadRpms();
    for (int port = 0; port < EuruxProtocol.PortCount; port++)
    {
        Console.WriteLine($"Port {port + 1}: {duties[port]}% PWM, {rpms[port]} RPM");
    }
}
