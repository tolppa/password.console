using password.classlibrary.Utils;

namespace password.console.tests
{
    public class RandomEntropyGeneratorTests
    {
        [Fact]
        public void GenerateRandomEntropy_DefaultLength_Returns32Bytes()
        {
            // Act
            var result = RandomEntropyGenerator.GenerateRandomEntropy();

            // Assert
            Assert.Equal(32, result.Length);
        }

        [Theory]
        [InlineData(64)]
        [InlineData(16)]
        [InlineData(1)]
        public void GenerateRandomEntropy_CustomLength_ReturnsCorrectLength(int length)
        {
            // Act
            var result = RandomEntropyGenerator.GenerateRandomEntropy(length);

            // Assert
            Assert.Equal(length, result.Length);
        }

        [Fact]
        public void GenerateRandomEntropy_TwoCalls_ReturnDifferentResults()
        {
            // Act
            var first = RandomEntropyGenerator.GenerateRandomEntropy();
            var second = RandomEntropyGenerator.GenerateRandomEntropy();

            // Assert
            Assert.False(first.SequenceEqual(second), "Consecutive entropy generations should produce different results");
        }

        [Fact]
        public void GenerateRandomEntropy_NegativeLength_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            const int invalidLength = -1;

            // Act & Assert
            Assert.Throws<OverflowException>(
                () => RandomEntropyGenerator.GenerateRandomEntropy(invalidLength)
            );
        }

        [Fact]
        public void GenerateRandomEntropy_ZeroLength_ReturnsEmptyArray()
        {
            // Act
            var result = RandomEntropyGenerator.GenerateRandomEntropy(0);

            // Assert
            Assert.Empty(result);
        }
    }
}