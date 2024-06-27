namespace Enigma5.App.Common.Utils;

public record class SingleThreadExecutorResult<T>(T? Value, Exception? Exception);
