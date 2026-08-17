using AsusEurux;

AssertEqual(
    "duty query",
    "900500",
    Convert.ToHexString(EuruxProtocol.CreateDutyQuery().AsSpan(0, 3)));

AssertEqual(
    "RPM query",
    "900501",
    Convert.ToHexString(EuruxProtocol.CreateRpmQuery().AsSpan(0, 3)));

AssertEqual(
    "set duty",
    "900400193264",
    Convert.ToHexString(EuruxProtocol.CreateSetDutyReport([0, 25, 50, 100]).AsSpan(0, 6)));

byte[] dutyResponse = new byte[EuruxProtocol.InputReportLength];
dutyResponse[0] = 0x90;
dutyResponse[1] = 0xF0;
dutyResponse[2] = 0x90;
dutyResponse[3] = 20;
dutyResponse[4] = 40;
dutyResponse[5] = 60;
dutyResponse[6] = 80;
AssertSequence<byte>("parse duty", [20, 40, 60, 80], EuruxProtocol.ParseDuties(dutyResponse));

byte[] rpmResponse = new byte[EuruxProtocol.InputReportLength];
rpmResponse[0] = 0x90;
rpmResponse[1] = 0xF1;
rpmResponse[2] = 0x04;
rpmResponse[3] = 0xD2;
rpmResponse[4] = 0x0A;
rpmResponse[5] = 0x28;
rpmResponse[6] = 0x00;
rpmResponse[7] = 0x00;
rpmResponse[8] = 0x12;
rpmResponse[9] = 0x34;
AssertSequence<ushort>("parse RPM", [1234, 2600, 0, 0x1234], EuruxProtocol.ParseRpms(rpmResponse));

AssertThrows<InvalidDataException>(
    "wrong response command",
    () => EuruxProtocol.ParseRpms(dutyResponse));

AssertThrows<InvalidDataException>(
    "invalid duty",
    () => EuruxProtocol.CreateSetDutyReport([0, 25, 50, 101]));

Console.WriteLine("All protocol tests passed.");

static void AssertEqual<T>(string name, T expected, T actual)
    where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected {expected}, got {actual}.");
    }
}

static void AssertSequence<T>(string name, IReadOnlyList<T> expected, IReadOnlyList<T> actual)
    where T : notnull
{
    if (!expected.SequenceEqual(actual))
    {
        throw new InvalidOperationException(
            $"{name}: expected [{string.Join(", ", expected)}], got [{string.Join(", ", actual)}].");
    }
}

static void AssertThrows<T>(string name, Action action)
    where T : Exception
{
    try
    {
        action();
    }
    catch (T)
    {
        return;
    }

    throw new InvalidOperationException($"{name}: expected {typeof(T).Name}.");
}
