using System.Collections;

namespace Enigma5.Message.Tests.TestData;

public class OnionBuilderTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { GenerateBytes(128), GenerateBytes(8), new byte[] { 1, 164 }, 422 };
        yield return new object[] { GenerateBytes(256), GenerateBytes(16), new byte[] { 2, 44 }, 558 };
        yield return new object[] { GenerateBytes(512), GenerateBytes(32), new byte[] { 3, 60 }, 830 };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static byte[] GenerateBytes(int count)
    {
        var bytes = new byte[count];
        new Random().NextBytes(bytes);
        return bytes;
    }
}
