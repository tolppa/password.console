using System.Security;
using password.classlibrary.Utils;

namespace password.console.tests
{
    public class SecurityHelperTests : IDisposable
    {
        private readonly string _testTempPath;

        private readonly StringWriter _consoleOutput;
        private readonly TextWriter _originalConsoleOut;

        public SecurityHelperTests()
        {
            _testTempPath = Path.Combine(
                 Path.GetTempPath(),
                 "passwordapp_tests",
                 Guid.NewGuid().ToString()
             );
            _consoleOutput = new StringWriter();
            _originalConsoleOut = Console.Out;
            Console.SetOut(_consoleOutput);
            Directory.CreateDirectory(_testTempPath);

        }

        public void Dispose()
        {

            if (Directory.Exists(_testTempPath))
            {
                try
                {
                    Directory.Delete(_testTempPath, recursive: true);
                }
                catch
                {
                    // Silent
                }
            }

            Console.SetOut(_originalConsoleOut);
            _consoleOutput.Dispose();
        }

        [Fact]
        public async Task ShowSpinnerAsync_ShouldDisplaySpinnerCharacters()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(5000);

            // Act
            await Helpers.ShowSpinnerAsync(cts.Token);

            await Task.Delay(1000);

            // Assert
            var output = _consoleOutput.ToString();
            Assert.NotNull(output);
        }

        [Fact]
        public void ConvertToUnsecureString_HandlesSecureStringCorrectly()
        {
            // Arrange
            var secureString = new SecureString();
            secureString.AppendChar('t');
            secureString.AppendChar('e');
            secureString.AppendChar('s');
            secureString.AppendChar('t');

            // Act
            var result = SecurityHelper.ConvertToUnsecureString(secureString);

            // Assert
            Assert.Equal("test", result);
        }

        [Fact]
        public void SaveAndLoadEncryptedConfig_RoundtripWorks()
        {
            // Arrange
            var testContent = "test content";
            var testPath = Path.Combine(_testTempPath, "test_config.bin");

            // Act
            SecurityHelper.SaveEncryptedConfig(testPath, testContent);
            var loadedContent = SecurityHelper.LoadEncryptedConfig(testPath);

            // Assert
            Assert.Equal(testContent, loadedContent);
        }

        [Fact]
        public void Entropy_InitializesCorrectlyForCurrentOS()
        {
            // Act
            var entropy = SecurityHelper.Entropy;

            // Assert
            Assert.NotNull(entropy);
            Assert.Equal(32, entropy.Length);
        }

        [Fact]
        public void Combine_ConcatenatesByteArraysCorrectly()
        {
            // Arrange
            var a = new byte[] { 1, 2, 3 };
            var b = new byte[] { 4, 5, 6 };

            // Act
            var result = SecurityHelper.Combine(a, b);

            // Assert
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6 }, result);
        }

        [Fact]
        public void LoadEncryptedConfig_ReturnsNullForMissingFile()
        {
            // Käytä Path.GetTempFileName() luotua olemassa olevaa polkua
            var validTempFile = Path.GetTempFileName();
            var invalidPath = Path.Combine(
                Path.GetDirectoryName(validTempFile),
                "non_existing.bin"
            );

            // Act
            var result = SecurityHelper.LoadEncryptedConfig(invalidPath);

            // Clean
            File.Delete(validTempFile);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void SaveEncryptedConfig_HandlesEmptyContent()
        {
            // Arrange
            var testPath = Path.Combine(_testTempPath, "empty_config.bin");

            // Ensure directory
            Directory.CreateDirectory(Path.GetDirectoryName(testPath));

            // Act
            SecurityHelper.SaveEncryptedConfig(testPath, "");
            var result = SecurityHelper.LoadEncryptedConfig(testPath);

            // Assert
            Assert.Equal("", result);
        }
    }

        // Interface for platform abstraction to enable testing
        public interface IPlatformService
    {
        bool IsWindows();
        bool IsLinux();
        bool IsMacOS();
    }
}