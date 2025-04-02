using Microsoft.Extensions.Logging;
using Moq;
using password.classlibrary.Models;
using password.classlibrary.Services;

namespace password.console.tests
{
    public class ConsoleServiceTests
    {

        public class TestKeyVaultService : KeyVaultService
        {
            public TestKeyVaultService()
                : base(
                    new KeyVaultSettings
                    {
                        CertificatePassword ="",
                        CertificatePath ="",
                        ClientId ="",
                        ClientSecret = "",
                        KeyVaultUrl = "https://test.vault.azure.net/",
                        TenantId = "",
                        UseManagedIdentity = false
                    },
                    new LoggingService(Mock.Of<ILogger<LoggingService>>())){}

        }

        // Testiversio LoggingService:stä
        public class TestLoggingService : LoggingService
        {
            public TestLoggingService()
                : base(Mock.Of<ILogger<LoggingService>>())
            {
            }

            public static new void LogOperation(string operation, string name)
            {
                // Ei tehdä mitään testeissä
            }
        }

        [Fact]
        public void Constructor_WorksWithRealServices()
        {
            // Arrange & Act
            var service = new ConsoleService(
                new TestKeyVaultService(),
                new TestLoggingService(),
                "https://test.vault.azure.net/",
                "test-config.json");

            // Assert
            Assert.NotNull(service);
        }
    }
}