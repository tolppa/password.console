using System.Security.Cryptography;

namespace password.classlibrary.Utils
{
    /// <summary>
    /// Provides a utility method for generating cryptographically secure random entropy.
    /// </summary>
    public static class RandomEntropyGenerator
    {
        /// <summary>
        /// Generates a cryptographically secure random byte array.
        /// </summary>
        /// <param name="length">The length of the entropy to generate in bytes (default is 32 bytes, which equals 256 bits).</param>
        /// <returns>A new byte array containing the generated random entropy.</returns>
        public static byte[] GenerateRandomEntropy(int length = 32)
        {
            using var rng = RandomNumberGenerator.Create();
            var entropy = new byte[length];
            rng.GetBytes(entropy);
            return entropy;
        }
    }
}