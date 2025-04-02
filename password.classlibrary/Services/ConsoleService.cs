using System;
using System.Threading.Tasks;

namespace password.classlibrary.Services
{
    /// <summary>
    /// A console service that always starts in interactive mode,
    /// using inherited logic from <see cref="BaseConsoleService"/>.
    /// </summary>
    public class ConsoleService : BaseConsoleService, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleService"/> class.
        /// </summary>
        /// <param name="keyVaultService">
        /// A configured <see cref="KeyVaultService"/> instance used for interacting with Azure Key Vault.
        /// </param>
        /// <param name="loggingService">
        /// The <see cref="LoggingService"/> instance to use for logging operations and errors.
        /// </param>
        /// <param name="keyVaultUrl">
        /// The initial Key Vault URL that the console application will connect to.
        /// </param>
        /// <param name="configPath">
        /// The file path to the configuration file used by the console application.
        /// </param>
        public ConsoleService(
            KeyVaultService keyVaultService,
            LoggingService loggingService,
            string keyVaultUrl,
            string configPath)
            : base(keyVaultService, loggingService, keyVaultUrl, configPath)
        {
        }

        /// <summary>
        /// Always runs the interactive console loop.
        /// This overrides the <see cref="BaseConsoleService.RunAsync"/> method to enforce interactive mode.
        /// </summary>
        public override async Task RunAsync()
        {
            await RunInteractiveConsole();
        }
    }
}