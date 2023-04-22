using Xunit;

namespace Crypto.Tests;

public class KeySequenceTests
{
    [Fact]
    public void KeySequence_CreatesSequenceWithCorrectSize()
    {
        // Arrange
        int size = 10;
        int keySize = 16;

        // Act
        var keySequence = new KeySequence(size, keySize);

        // Assert
        Assert.Equal(size, keySequence.Count);
    }

    [Fact]
    public void KeySequence_ContainsByteSequencesOfCorrectSize()
    {
        // Arrange
        int size = 10;
        int keySize = 16;

        // Act
        var keySequence = new KeySequence(size, keySize);

        // Assert
        foreach (var key in keySequence)
        {
            Assert.Equal(keySize, ((byte[])key).Length);
        }
    }

    [Fact]
    public void GenerateByteSequences_ReturnsSequencesOfCorrectSize()
    {
        // Arrange
        int size = 10;
        int keySize = 16;

        // Act
        var byteSequences = KeySequence.GenerateByteSequences(size, keySize);

        // Assert
        foreach (var sequence in byteSequences)
        {
            Assert.Equal(keySize, sequence.Length);
        }
    }

    [Fact]
    public void GenerateByteSequences_ReturnsUniqueSequences()
    {
        // Arrange
        int size = 10;
        int keySize = 16;

        // Act
        var byteSequences1 = KeySequence.GenerateByteSequences(size, keySize);
        var byteSequences2 = KeySequence.GenerateByteSequences(size, keySize);

        // Assert
        Assert.NotEqual(byteSequences1, byteSequences2);
        Assert.True(byteSequences1.SequenceEqual(byteSequences1.Distinct()));
        Assert.True(byteSequences2.SequenceEqual(byteSequences2.Distinct()));
    }
}
