using Microsoft.Extensions.Logging;
using password.classlibrary.Interfaces;
using System;
using System.Security.Cryptography;
using System.Text;

namespace password.classlibrary.Services
{
    /// <summary>
    /// Provides functionality to log operations and errors for the password console application.
    /// Implements the <see cref="ILoggingService"/> and <see cref="IDisposable"/> interfaces.
    /// </summary>
    public class LoggingService : ILoggingService, IDisposable
    {
        /// <summary>
        /// A reference to the Microsoft.Extensions.Logging logger used to record log messages.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingService"/> class.
        /// </summary>
        /// <param name="logger">
        /// The strongly-typed logger for this service (<see cref="LoggingService"/>).
        /// This logger instance is typically injected via dependency injection.
        /// </param>
        public LoggingService(ILogger<LoggingService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Demonstrates how to create a hashed version of the provided name.
        /// The hashed name is calculated but not logged or stored in this implementation.
        /// This method serves as an example of how sensitive data could be processed securely.
        /// </summary>
        /// <param name="operation">A short string describing the operation being performed.</param>
        /// <param name="name">The raw name or identifier for which to compute a hash.</param>
        public void LogOperation(string operation, string name)
        {
            // Compute a SHA256 hash of the name for demonstration.
            // This hashedName is not logged or persisted in this sample code.
            var hashedName = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(name)));
            // In a real application, you might log the operation and a non-sensitive identifier.
            // Example: _logger.LogInformation($"Operation: {operation} | Hashed Identifier: {hashedName}");
        }

        /// <summary>
        /// Logs an error message to the configured logging output, including the current user name.
        /// </summary>
        /// <param name="error">A brief description of the error that occurred.</param>
        public void LogError(string error)
        {
            _logger.LogError($"Error: {error} | User: {Environment.UserName}");
        }

        /// <summary>
        /// Disposes of the resources held by this instance.
        /// Specifically, it disposes of the underlying logger if it implements the <see cref="IDisposable"/> interface.
        /// </summary>
        public void Dispose()
        {
            (_logger as IDisposable)?.Dispose();
        }
    }
}