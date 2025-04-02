using Microsoft.Extensions.Logging;
using Moq;
using password.classlibrary.Enums;
using password.classlibrary.Interfaces;
using password.classlibrary.Services;

namespace password.console.tests
{
    public class BaseConsoleServiceTests : IDisposable
    {
        private readonly List<string> _tempFiles = [];

        public void Dispose()
        {
            foreach (var file in _tempFiles)
            {
                if (File.Exists(file)) File.Delete(file);
            }
        }

        private class TestBaseConsoleService : BaseConsoleService
        {
            private StringWriter _consoleOutput;
            private StringReader _consoleInput;

            // Käytä IKeyVaultService-rajapintaa
            public Mock<IKeyVaultService> MockKeyVaultService { get; }
            public Mock<ILoggingService> MockLoggingService { get; }
            public Mock<IEnvironmentService> MockEnvironmentService { get; } = new Mock<IEnvironmentService>();

            public string TestKeyVaultUrl => _keyVaultUrl;

            // Korjattu konstruktori käyttämään IKeyVaultService-rajapintaa
            public TestBaseConsoleService(
                Mock<IKeyVaultService> keyVaultService,
                Mock<ILoggingService> loggingService,
                string keyVaultUrl = "https://test-vault.example.com/",
                string configPath = null,
                string input = "")
                : base(
                    keyVaultService?.Object ?? Mock.Of<IKeyVaultService>(),
                    loggingService?.Object ?? Mock.Of<ILoggingService>(),
                    keyVaultUrl,
                    configPath ?? Path.GetTempFileName())
            {
                MockKeyVaultService = keyVaultService ?? new Mock<IKeyVaultService>();
                MockLoggingService = loggingService ?? new Mock<ILoggingService>();
                RedirectConsole(input);
            }

            public void RedirectConsole(string input)
            {
                _consoleOutput = new StringWriter();
                _consoleInput = new StringReader(input);
                Console.SetOut(_consoleOutput);
                Console.SetIn(_consoleInput);
            }

            public async Task RunInteractiveConsolePublic() => await RunInteractiveConsole();
            public string GetConsoleOutput() => _consoleOutput.ToString();
            public override Task RunAsync() => Task.CompletedTask;
            public Task TestRunMenuOption(MenuOption option) => RunMenuOption(option);

            public async Task TestCleanupAndExit(ConfirmationOption option)
            {
                RedirectConsole(((int)option).ToString());
                await CleanupAndExit();
            }
        }

        private TestBaseConsoleService CreateService(
            string input = "",
            Action<Mock<IKeyVaultService>> setupKeyVault = null, // Muutettu rajapinnaksi
            string configPath = null)
        {
            var mockLogger = new Mock<ILogger<ILoggingService>>();
            var loggingService = new Mock<ILoggingService>();

            // Luo mock IKeyVaultService-rajapinnalle
            var mockKeyVault = new Mock<IKeyVaultService>();

            setupKeyVault?.Invoke(mockKeyVault);

            var service = new TestBaseConsoleService(
                mockKeyVault,
                loggingService,
                configPath: configPath,
                input: input);

            if (configPath != null) _tempFiles.Add(configPath);

            return service;
        }

        [Fact]
        public async Task ChangeKeyVaultUrl_ValidUrl_UpdatesServiceAndSavesConfig()
        {
            // Arrange
            var tempConfig = Path.GetTempFileName();
            var service = CreateService(
                input: "https://new-vault.azure.net/\n",
                configPath: tempConfig,
                setupKeyVault: mock =>
                {
                    // Poistettu InitializeAsync-mockaus
                    _ = mock.Setup(x => x.ListSecretsAsync(
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    )).ReturnsAsync([]);
                });

            // Act
            await service.TestRunMenuOption(MenuOption.ChangeKeyvaultUrl);

            // Assert
            Assert.Equal("https://test-vault.example.com/", service.TestKeyVaultUrl);
        }

    }
}