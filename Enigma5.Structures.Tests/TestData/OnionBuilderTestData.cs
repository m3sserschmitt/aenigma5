using System.Collections;

namespace Enigma5.Structures.Tests.TestData;

public class OnionBuilderTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { GenerateBytes(128), GenerateBytes(32), new byte[] { 1, 188 }, 446 };
        yield return new object[] { GenerateBytes(256), GenerateBytes(32), new byte[] { 2, 60 }, 574 };
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