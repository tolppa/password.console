using Moq;
using Azure.Security.KeyVault.Secrets;
using password.classlibrary.Interfaces;
using password.classlibrary.Models;
using Azure;
using System.Reflection;

namespace password.classlibrary.Services.Tests
{
    public class KeyVaultServiceTests
    {
        private class KeyVaultServiceHarness : KeyVaultService
        {
            public KeyVaultServiceHarness(KeyVaultSettings settings, ILoggingService logging)
                : base(settings, logging) { }

            public void SetTestDependencies(
                SecretClient client,
                bool initialized,
                Dictionary<string, bool> permissions)
            {
                typeof(KeyVaultService)
                    .GetField("_secretClient", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(this, client);

                typeof(KeyVaultService)
                    .GetField("_initialized", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(this, initialized);

                typeof(KeyVaultService)
                    .GetField("_userPermissions", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(this, permissions);
            }
        }

        private readonly Mock<ILoggingService> _mockLogger = new Mock<ILoggingService>();
        private readonly KeyVaultSettings _validSettings = new KeyVaultSettings
        {
            KeyVaultUrl = "https://passwordappvault.vault.azure.net/",
            UseManagedIdentity = true
        };

        [Fact]
        public async Task GetSecretAsync_NonExistingSecret_ThrowsKeyNotFoundException()
        {
            // Arrange
            var mockClient = new Mock<SecretClient>();
            mockClient.Setup(c => c.GetSecretAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException(404, "Not Found"));

            var harness = new KeyVaultServiceHarness(_validSettings, _mockLogger.Object);
            harness.SetTestDependencies(
                mockClient.Object,
                true,
                new Dictionary<string, bool> { { "Get", true } });

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => harness.GetSecretAsync("test-secret"));

            _mockLogger.Verify(x => x.LogOperation("Fetching password", "test-secret"), Times.Once);
        }

        [Fact]
        public async Task GetSecretAsync_ExistingSecret_ReturnsSecretValue()
        {
            // Arrange
            var mockClient = new Mock<SecretClient>();
            var expectedSecret = new KeyVaultSecret("test-secret", "test-value");
            mockClient.Setup(c => c.GetSecretAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(expectedSecret, Mock.Of<Response>()));

            var harness = new KeyVaultServiceHarness(_validSettings, _mockLogger.Object);
            harness.SetTestDependencies(
                mockClient.Object,
                true,
                new Dictionary<string, bool> { { "Get", true } });

            // Act
            var result = await harness.GetSecretAsync("test-secret");

            // Assert
            Assert.Equal("test-value", result);
            _mockLogger.Verify(x => x.LogOperation("Fetching password", "test-secret"), Times.Once);
        }

        [Fact]
        public async Task AddSecretAsync_SuccessfulAdd_LogsOperation()
        {
            // Arrange
            var mockClient = new Mock<SecretClient>();
            mockClient.Setup(c => c.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret("test-secret", "test-value"), Mock.Of<Response>()));

            var harness = new KeyVaultServiceHarness(_validSettings, _mockLogger.Object);
            harness.SetTestDependencies(
                mockClient.Object,
                true,
                new Dictionary<string, bool> { { "Set", true } });

            // Act
            await harness.AddSecretAsync("test-secret", "test-value");

            // Assert
            _mockLogger.Verify(x => x.LogOperation("AddSecret", "test-secret"), Times.Once);
        }

        [Fact]
        public async Task AddSecretAsync_RequestFailed_ThrowsExceptionAndLogsError()
        {
            // Arrange
            var mockClient = new Mock<SecretClient>();
            mockClient.Setup(c => c.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException(400, "Bad Request"));

            var harness = new KeyVaultServiceHarness(_validSettings, _mockLogger.Object);
            harness.SetTestDependencies(
                mockClient.Object,
                true,
                new Dictionary<string, bool> { { "Set", true } });

            // Act & Assert
            await Assert.ThrowsAsync<RequestFailedException>(() => harness.AddSecretAsync("test-secret", "test-value"));
            _mockLogger.Verify(x => x.LogError(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task UpdateSecretAsync_SuccessfulUpdate_LogsOperation()
        {
            // Arrange
            var mockClient = new Mock<SecretClient>();
            mockClient.Setup(c => c.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret("test-secret", "new-value"), Mock.Of<Response>()));

            var harness = new KeyVaultServiceHarness(_validSettings, _mockLogger.Object);
            harness.SetTestDependencies(
                mockClient.Object,
                true,
                new Dictionary<string, bool> { { "Set", true } });

            // Act
            await harness.UpdateSecretAsync("test-secret", "new-value");

            // Assert
            _mockLogger.Verify(x => x.LogOperation("UpdateSecret", "test-secret"), Times.Once);
        }

        [Fact]
        public async Task UpdateSecretAsync_RequestFailed_ThrowsExceptionAndLogsError()
        {
            // Arrange
            var mockClient = new Mock<SecretClient>();
            mockClient.Setup(c => c.SetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException(400, "Bad Request"));

            var harness = new KeyVaultServiceHarness(_validSettings, _mockLogger.Object);
            harness.SetTestDependencies(
                mockClient.Object,
                true,
                new Dictionary<string, bool> { { "Set", true } });

            // Act & Assert
            await Assert.ThrowsAsync<RequestFailedException>(() => harness.UpdateSecretAsync("test-secret", "new-value"));
            _mockLogger.Verify(x => x.LogError(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DeleteSecretAsync_RequestFailed_ThrowsExceptionAndLogsError()
        {
            // Arrange
            var mockClient = new Mock<SecretClient>();
            mockClient.Setup(c => c.StartDeleteSecretAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException(400, "Bad Request"));

            var harness = new KeyVaultServiceHarness(_validSettings, _mockLogger.Object);
            harness.SetTestDependencies(
                mockClient.Object,
                true,
                new Dictionary<string, bool> { { "Delete", true } });

            // Act & Assert
            await Assert.ThrowsAsync<RequestFailedException>(() => harness.DeleteSecretAsync("test-secret"));
            _mockLogger.Verify(x => x.LogError(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task RestoreSecretAsync_RequestFailed_ThrowsExceptionAndLogsError()
        {
            // Arrange
            var mockClient = new Mock<SecretClient>();
            mockClient.Setup(c => c.StartRecoverDeletedSecretAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException(400, "Bad Request"));

            var harness = new KeyVaultServiceHarness(_validSettings, _mockLogger.Object);
            harness.SetTestDependencies(
                mockClient.Object,
                true,
                new Dictionary<string, bool> { { "Recover", true } });

            // Act & Assert
            await Assert.ThrowsAsync<RequestFailedException>(() => harness.RestoreSecretAsync("test-secret"));
            _mockLogger.Verify(x => x.LogError(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task PurgeSecretAsync_RequestFailed_ThrowsExceptionAndLogsError()
        {
            // Arrange
            var mockClient = new Mock<SecretClient>();
            mockClient.Setup(c => c.PurgeDeletedSecretAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException(400, "Bad Request"));

            var harness = new KeyVaultServiceHarness(_validSettings, _mockLogger.Object);
            harness.SetTestDependencies(
                mockClient.Object,
                true,
                new Dictionary<string, bool> { { "Purge", true } });

            // Act & Assert
            await Assert.ThrowsAsync<RequestFailedException>(() => harness.PurgeSecretAsync("test-secret"));
            _mockLogger.Verify(x => x.LogError(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SecretExistsAsync_ExistingSecret_ReturnsTrue()
        {
            // Arrange
            var mockClient = new Mock<SecretClient>();
            mockClient.Setup(c => c.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response.FromValue(new KeyVaultSecret("test-secret", "test-value"), Mock.Of<Response>()));

            var harness = new KeyVaultServiceHarness(_validSettings, _mockLogger.Object);
            harness.SetTestDependencies(
                mockClient.Object,
                true,
                new Dictionary<string, bool> { { "Get", true } });

            // Act
            var result = await harness.SecretExistsAsync("test-secret");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task SecretExistsAsync_NonExistingSecret_ReturnsFalse()
        {
            // Arrange
            var mockClient = new Mock<SecretClient>();
            mockClient.Setup(c => c.GetSecretAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException(404, "Not Found"));

            var harness = new KeyVaultServiceHarness(_validSettings, _mockLogger.Object);
            harness.SetTestDependencies(
                mockClient.Object,
                true,
                new Dictionary<string, bool> { { "Get", true } });

            // Act
            var result = await harness.SecretExistsAsync("test-secret");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task SecretExistsAsync_RequestFailedOtherThanNotFound_ThrowsExceptionAndLogsError()
        {
            // Arrange
            var mockClient = new Mock<SecretClient>();
            mockClient.Setup(c => c.GetSecretAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RequestFailedException(400, "Bad Request"));

            var harness = new KeyVaultServiceHarness(_validSettings, _mockLogger.Object);
            harness.SetTestDependencies(
                mockClient.Object,
                true,
                new Dictionary<string, bool> { { "Get", true } });

            // Act & Assert
            await Assert.ThrowsAsync<RequestFailedException>(() => harness.SecretExistsAsync("test-secret"));
            _mockLogger.Verify(x => x.LogError(It.IsAny<string>()), Times.Once);
        }
    }

    public interface IDeleteOperation
    {
        Task<Response<DeletedSecret>> WaitForCompletionAsync(CancellationToken cancellationToken);
    }
    public class TestDeletedSecret
    {
        public TestDeletedSecret(SecretProperties properties)
        {
            SecretProperties = properties;
        }

        public SecretProperties SecretProperties { get; }
        public DateTimeOffset? DeletedOn { get; set; }
        public DateTimeOffset? ScheduledPurgeDate { get; set; }
        public string RecoveryId { get; set; }
        public string Name => SecretProperties?.Name;
    }

}