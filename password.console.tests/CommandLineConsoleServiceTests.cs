using Microsoft.Extensions.Logging;
using Moq;
using password.classlibrary.Models;
using password.classlibrary.Services;

namespace password.console.tests
{
    public class CommandLineConsoleServiceTests
    {

        private class TestableCommandLineConsoleService : CommandLineConsoleService
        {
            private readonly string[] _testArgs;

            public TestableCommandLineConsoleService(
                KeyVaultService keyVaultService,
                LoggingService loggingService,
                string[] testArgs)
                : base(keyVaultService, loggingService, "https://test-vault.example.com/", "test-config.json")
            {
                _testArgs = testArgs;
            }

            public override async Task RunAsync()
            {
                if (_testArgs.Length > 0)
                {
                    await HandleCommandLineArguments(_testArgs);
                }
                else
                {
                    await base.RunAsync();
                }
            }
        }

        private class FakeKeyVaultService : KeyVaultService
        {
            public FakeKeyVaultService()
                : base(
                    new KeyVaultSettings
                    {
                        KeyVaultUrl = "https://test-vault.example.com/"
                    },
                    new LoggingService(Mock.Of<ILogger<LoggingService>>()))
            {
            }

            public override Task<string> GetSecretAsync(string name)
            {
                return Task.FromResult("test-secret-value");
            }

            public override Task<List<string>> ListSecretsAsync(string searchTerm, CancellationToken token)
            {
                return Task.FromResult(new List<string> { "secret1", "secret2" });
            }
        }

        [Fact]
        public async Task RunAsync_WithGetCommand_DisplaysSecret()
        {
            // Arrange
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            var service = new TestableCommandLineConsoleService(
                new FakeKeyVaultService(),
                new LoggingService(Mock.Of<ILogger<LoggingService>>()),
                ["-get", "test-secret"]);

            // Act
            await service.RunAsync();

            // Assert
            Assert.Contains("test-secret-value", consoleOutput.ToString());
        }

        [Fact]
        public async Task RunAsync_WithListCommand_DisplaysSecrets()
        {
            // Arrange
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            var service = new TestableCommandLineConsoleService(
                new FakeKeyVaultService(),
                new LoggingService(Mock.Of<ILogger<LoggingService>>()),
                ["-list"]);

            // Act
            await service.RunAsync();

            // Assert
            Assert.Contains("secret1", consoleOutput.ToString());
            Assert.Contains("secret2", consoleOutput.ToString());
        }

        [Fact]
        public async Task RunAsync_WithInvalidCommand_ShowsError()
        {
            // Arrange
            var consoleOutput = new StringWriter();
            Console.SetOut(consoleOutput);

            var service = new TestableCommandLineConsoleService(
                new FakeKeyVaultService(),
                new LoggingService(Mock.Of<ILogger<LoggingService>>()),
                ["-invalid"]);

            // Act
            await service.RunAsync();

            // Assert
            Assert.Contains("Unknown command", consoleOutput.ToString());
        }
    }
}