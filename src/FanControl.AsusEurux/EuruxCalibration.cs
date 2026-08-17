namespace FanControl.AsusEurux;

internal static class EuruxCalibration
{
    private const byte ProtocolMinimumDuty = 1;
    private const byte MinimumRunningDuty = 5;
    private const int MinimumRunningRpm = 300;
    private const int MaximumRpm = 2250;

    public static byte ToProtocolDuty(byte requestedDuty)
    {
        return requestedDuty == 0 ? ProtocolMinimumDuty : requestedDuty;
    }

    public static ushort EstimateRpm(byte requestedDuty)
    {
        if (requestedDuty < MinimumRunningDuty)
        {
            return 0;
        }

        double progress = (requestedDuty - MinimumRunningDuty) / (double)(100 - MinimumRunningDuty);
        double rpm = MinimumRunningRpm + progress * (MaximumRpm - MinimumRunningRpm);
        return (ushort)Math.Round(rpm, MidpointRounding.AwayFromZero);
    }
}
