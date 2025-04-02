using Microsoft.Extensions.Logging;
using password.classlibrary.Interfaces;
using password.classlibrary.Models;
using password.classlibrary.Services;
using password.classlibrary.Utils;

namespace password.console
{
    /// <summary>
    /// Main entry point for the Password Manager console application
    /// </summary>
    public static class Program
    {
        private const string ConfigFileName = "azureConfig.secure";
        private static string _configPath;
        private static KeyVaultSettings _vaultSettings;

        /// <summary>
        /// Application entry point with async support
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static async Task Main(string[] args)
        {
            // Configure logging infrastructure first
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddAzureWebAppDiagnostics(); // For Azure integration
            });

            var logger = loggerFactory.CreateLogger<LoggingService>();
            var loggingService = new LoggingService(logger);

            // Initialize environment configuration service
            var envService = new EnvironmentService();

            // Set up secure configuration file path
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PasswordManager",
                ConfigFileName
            );

            // Offer path registration for first-time users
            if (!File.Exists(_configPath))
            {
                Console.WriteLine("\nDo you want to register the application for easier system access?");
                Console.WriteLine("1. Yes\n2. No");
                if (Console.ReadLine() == "1") envService.RegisterApplicationPath(false);
            }

            // Establish secure connection to Azure Key Vault
            if (!await InitializeKeyVaultConnection(loggingService))
            {
                Helpers.PrintError("Application terminated due to critical error");
                Environment.Exit(1);
            }

            // Initialize core services with dependency injection
            var keyVaultService = new KeyVaultService(_vaultSettings, loggingService);
            var consoleService = new CommandLineConsoleService(
                keyVaultService,
                loggingService,
                _vaultSettings.KeyVaultUrl,
                _configPath
            );

            // Start main application loop
            await consoleService.RunAsync();
        }

        #region Key Vault Initialization
        /// <summary>
        /// Handles Key Vault connection initialization with fallback logic
        /// </summary>
        private static async Task<bool> InitializeKeyVaultConnection(ILoggingService loggingService)
        {
            try
            {
                return File.Exists(_configPath)
                    ? await LoadSavedConfiguration(loggingService)
                    : await ConfigureFirstTimeSetup(loggingService);
            }
            catch (Exception ex)
            {
                loggingService.LogError($"Initialization error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads and validates existing encrypted configuration
        /// </summary>
        private static async Task<bool> LoadSavedConfiguration(ILoggingService loggingService)
        {
            try
            {
                var configData = SecurityHelper.LoadEncryptedConfig(_configPath);
                var configParts = configData.Split('|');

                _vaultSettings = new KeyVaultSettings
                {
                    KeyVaultUrl = configParts[0]
                };

                // Parse configuration based on stored authentication type
                if (configParts.Length > 1 && configParts[1] == "managed")
                {
                    _vaultSettings.UseManagedIdentity = true;
                }
                else if (configParts.Length > 3 && configParts[3] == "cert")
                {
                    // Certificate-based authentication configuration
                    _vaultSettings.TenantId = configParts[1];
                    _vaultSettings.ClientId = configParts[2];
                    _vaultSettings.CertificatePath = configParts[4];
                    _vaultSettings.CertificatePassword = configParts[5];
                }
                else if (configParts.Length > 3)
                {
                    // Client secret configuration
                    _vaultSettings.TenantId = configParts[1];
                    _vaultSettings.ClientId = configParts[2];
                    _vaultSettings.ClientSecret = configParts[3];
                }

                // Validate configuration with actual Key Vault connection
                var keyVaultService = new KeyVaultService(_vaultSettings, loggingService);
                await keyVaultService.InitializeAsync();
                return true;
            }
            catch (Exception ex)
            {
                loggingService.LogError($"Configuration load failed: {ex.Message}");
                return await HandleInvalidConfiguration(loggingService);
            }
        }

        /// <summary>
        /// Guides user through initial authentication configuration
        /// </summary>
        private static async Task<bool> ConfigureFirstTimeSetup(ILoggingService loggingService)
        {
            Console.WriteLine("\nSelect authentication method:");
            Console.WriteLine("1. Azure AD Application (Client Secret)");
            Console.WriteLine("2. Azure AD Application (Certificate)");
            Console.WriteLine("3. Interactive Azure Login");
            Console.WriteLine("4. Managed Identity (Azure Environments)");
            Console.Write("Your choice: ");

            var choice = Console.ReadLine();

            return choice switch
            {
                "1" => await ConfigureAzureAdCredentials(loggingService),
                "2" => await ConfigureCertificateAuth(loggingService),
                "3" => await ConfigureAutomaticAuthentication(loggingService),
                "4" => ConfigureManagedIdentity(loggingService),
                _ => false
            };
        }
        #endregion

        #region Authentication Configuration Methods
        /// <summary>
        /// Configures certificate-based authentication with user input
        /// </summary>
        private static async Task<bool> ConfigureCertificateAuth(ILoggingService loggingService)
        {
            _vaultSettings = new KeyVaultSettings
            {
                TenantId = Helpers.ReadInput("Azure Tenant ID"),
                ClientId = Helpers.ReadInput("Client ID"),
                KeyVaultUrl = Helpers.ReadInput("Key Vault URL"),
                CertificatePath = Helpers.ReadInput("Certificate file path (.pfx)"),
                CertificatePassword = Helpers.ReadPassword("Certificate password")
            };

            try
            {
                // Test certificate authentication
                var keyVaultService = new KeyVaultService(_vaultSettings, loggingService);
                await keyVaultService.InitializeAsync();

                // Save successful configuration
                SaveConfiguration($"{_vaultSettings.KeyVaultUrl}|{_vaultSettings.TenantId}|{_vaultSettings.ClientId}|cert|{_vaultSettings.CertificatePath}|{_vaultSettings.CertificatePassword}");
                return true;
            }
            catch (Exception ex)
            {
                loggingService.LogError($"Certificate configuration error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Configures Managed Identity for Azure-hosted environments
        /// </summary>
        private static bool ConfigureManagedIdentity(ILoggingService loggingService)
        {
            _vaultSettings = new KeyVaultSettings
            {
                KeyVaultUrl = Helpers.ReadInput("Key Vault URL"),
                UseManagedIdentity = true
            };

            try
            {
                // Validate Managed Identity availability
                var keyVaultService = new KeyVaultService(_vaultSettings, loggingService);
                keyVaultService.InitializeAsync().Wait(); // Synchronous wait for initial validation
                SaveConfiguration($"{_vaultSettings.KeyVaultUrl}|managed");
                return true;
            }
            catch (Exception ex)
            {
                loggingService.LogError($"Managed Identity connection error: {ex.Message}");
                Console.WriteLine("Verification checklist:");
                Console.WriteLine("- Running in Azure environment (VM, App Service, etc.)");
                Console.WriteLine("- Managed Identity is enabled");
                Console.WriteLine("- Proper Key Vault access permissions");
                return false;
            }
        }

        /// <summary>
        /// Configures client secret authentication
        /// </summary>
        private static async Task<bool> ConfigureAzureAdCredentials(ILoggingService loggingService)
        {
            _vaultSettings = new KeyVaultSettings
            {
                TenantId = Helpers.ReadInput("Azure Tenant ID"),
                ClientId = Helpers.ReadInput("Client ID"),
                ClientSecret = Helpers.ReadPassword("Client Secret"),
                KeyVaultUrl = Helpers.ReadInput("Key Vault URL")
            };

            try
            {
                var keyVaultService = new KeyVaultService(_vaultSettings, loggingService);
                await keyVaultService.InitializeAsync();
                SaveConfiguration($"{_vaultSettings.KeyVaultUrl}|{_vaultSettings.TenantId}|{_vaultSettings.ClientId}|{_vaultSettings.ClientSecret}");
                return true;
            }
            catch (Exception ex)
            {
                loggingService.LogError($"Configuration error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Configures interactive authentication flow
        /// </summary>
        private static async Task<bool> ConfigureAutomaticAuthentication(ILoggingService loggingService)
        {
            _vaultSettings = new KeyVaultSettings
            {
                KeyVaultUrl = Helpers.ReadInput("Key Vault URL")
            };

            try
            {
                var keyVaultService = new KeyVaultService(_vaultSettings, loggingService);
                await keyVaultService.InitializeAsync();
                SaveConfiguration(_vaultSettings.KeyVaultUrl);
                return true;
            }
            catch (Exception ex)
            {
                loggingService.LogError($"Connection error: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Configuration Helpers
        /// <summary>
        /// Securely saves configuration using platform-specific encryption
        /// </summary>
        private static void SaveConfiguration(string configData)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath));
            SecurityHelper.SaveEncryptedConfig(_configPath, configData);
        }

        /// <summary>
        /// Handles invalid configuration scenarios with recovery options
        /// </summary>
        private static async Task<bool> HandleInvalidConfiguration(ILoggingService loggingService)
        {
            Helpers.PrintError("Invalid configuration detected");
            Console.WriteLine("Choose action:");
            Console.WriteLine("1. Retry initialization");
            Console.WriteLine("2. Reset configuration");

            return Console.ReadLine() switch
            {
                "1" => await InitializeKeyVaultConnection(loggingService),
                "2" => await DeleteAndReconfigure(loggingService),
                _ => false
            };
        }

        /// <summary>
        /// Resets configuration and restarts setup process
        /// </summary>
        private static async Task<bool> DeleteAndReconfigure(ILoggingService loggingService)
        {
            try
            {
                if (File.Exists(_configPath)) File.Delete(_configPath);
                return await ConfigureFirstTimeSetup(loggingService);
            }
            catch (Exception ex)
            {
                loggingService.LogError($"Reset failed: {ex.Message}");
                return false;
            }
        }
        #endregion
    }
}