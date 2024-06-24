namespace Enigma5.App.Common.Constants;

public static class DataPersistencePeriod
{
    public static readonly TimeSpan PendingMessagePersistancePeriod = new(24, 0, 0);

    public static readonly TimeSpan SharedDataPersistancePeriod = new(0, 15, 0);
}
