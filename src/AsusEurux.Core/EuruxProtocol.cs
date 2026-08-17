using System.IO;

namespace AsusEurux;

/// <summary>
/// ROG EURUX GR120 controller HID reports, derived from ENE Fan HAL 1.0.18.1.
/// </summary>
public static class EuruxProtocol
{
    public const int VendorId = 0x0B05;
    public const int ProductId = 0x1D98;
    public const byte ReportId = 0x90;
    public const int PortCount = 4;
    public const int FeatureReportLength = 32;
    public const int InputReportLength = 65;

    private const byte QueryCommand = 0x05;
    private const byte QueryDutySubcommand = 0x00;
    private const byte QueryRpmSubcommand = 0x01;
    private const byte SetDutyCommand = 0x04;
    private const byte DutyResponse = 0xF0;
    private const byte RpmResponse = 0xF1;

    public static byte[] CreateDutyQuery()
    {
        byte[] report = CreateFeatureReport();
        report[1] = QueryCommand;
        report[2] = QueryDutySubcommand;
        return report;
    }

    public static byte[] CreateRpmQuery()
    {
        byte[] report = CreateFeatureReport();
        report[1] = QueryCommand;
        report[2] = QueryRpmSubcommand;
        return report;
    }

    public static byte[] CreateSetDutyReport(ReadOnlySpan<byte> duties)
    {
        ValidateDuties(duties);

        byte[] report = CreateFeatureReport();
        report[1] = SetDutyCommand;
        duties.CopyTo(report.AsSpan(2, PortCount));
        return report;
    }

    public static byte[] ParseDuties(ReadOnlySpan<byte> response)
    {
        // Firmware release 0.6 echoes Report ID 0x90 once more before the four duties.
        ValidateResponse(response, DutyResponse, 3 + PortCount);
        if (response[2] != ReportId)
        {
            throw new InvalidDataException(
                $"Unexpected EURUX duty payload marker 0x{response[2]:X2}; expected 0x{ReportId:X2}.");
        }

        byte[] duties = response.Slice(3, PortCount).ToArray();
        try
        {
            ValidateDuties(duties);
        }
        catch (InvalidDataException exception)
        {
            string prefix = Convert.ToHexString(response[..Math.Min(response.Length, 16)]);
            throw new InvalidDataException($"{exception.Message} Response prefix: {prefix}.", exception);
        }

        return duties;
    }

    public static ushort[] ParseRpms(ReadOnlySpan<byte> response)
    {
        ValidateResponse(response, RpmResponse, 2 + PortCount * 2);

        ushort[] rpms = new ushort[PortCount];
        for (int port = 0; port < PortCount; port++)
        {
            int offset = 2 + port * 2;
            rpms[port] = (ushort)((response[offset] << 8) | response[offset + 1]);
        }

        return rpms;
    }

    private static byte[] CreateFeatureReport()
    {
        byte[] report = new byte[FeatureReportLength];
        report[0] = ReportId;
        return report;
    }

    private static void ValidateDuties(ReadOnlySpan<byte> duties)
    {
        if (duties.Length != PortCount)
        {
            throw new ArgumentException($"Expected {PortCount} duty values.", nameof(duties));
        }

        for (int port = 0; port < duties.Length; port++)
        {
            if (duties[port] > 100)
            {
                throw new InvalidDataException(
                    $"Controller returned an invalid PWM duty for port {port + 1}: {duties[port]}.");
            }
        }
    }

    private static void ValidateResponse(ReadOnlySpan<byte> response, byte expectedCommand, int minimumLength)
    {
        if (response.Length < minimumLength)
        {
            throw new InvalidDataException(
                $"EURUX response is too short: {response.Length}, expected at least {minimumLength} bytes.");
        }

        if (response[0] != ReportId)
        {
            throw new InvalidDataException(
                $"Unexpected EURUX report ID 0x{response[0]:X2}; expected 0x{ReportId:X2}.");
        }

        if (response[1] != expectedCommand)
        {
            throw new InvalidDataException(
                $"Unexpected EURUX response 0x{response[1]:X2}; expected 0x{expectedCommand:X2}.");
        }
    }
}
