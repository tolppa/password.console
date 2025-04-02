using Microsoft.Extensions.Logging;
using Moq;
using password.classlibrary.Services;

namespace password.console.Tests
{
    public class LoggingServiceTests
    {
        [Fact]
        public void LogOperation_WithValidName_DoesNotThrow()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<LoggingService>>();
            var service = new LoggingService(mockLogger.Object);
            string operation = "testOp";
            string name = "testName";

            // Act
            var exception = Record.Exception(() => service.LogOperation(operation, name));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void LogOperation_WithNullName_ThrowsArgumentNullException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<LoggingService>>();
            var service = new LoggingService(mockLogger.Object);
            string operation = "testOp";
            string name = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => service.LogOperation(operation, name));
        }

        [Fact]
        public void LogOperation_WithEmptyName_DoesNotThrow()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<LoggingService>>();
            var service = new LoggingService(mockLogger.Object);
            string operation = "testOp";
            string name = "";

            // Act
            var exception = Record.Exception(() => service.LogOperation(operation, name));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void LogError_LogsCorrectMessage()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<LoggingService>>();
            var service = new LoggingService(mockLogger.Object);
            string errorMessage = "test error";
            string expectedMessage = $"Error: {errorMessage} | User: {Environment.UserName}";

            // Act
            service.LogError(errorMessage);

            // Assert
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() == expectedMessage),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}