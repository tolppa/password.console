using Azure;
using System;
using System.IO;
using System.Threading.Tasks;
using password.classlibrary.Enums;
using password.classlibrary.Interfaces;
using password.classlibrary.Models;
using password.classlibrary.Utils;

namespace password.classlibrary.Services
{
    /// <summary>
    /// A base class that provides shared console-related operations and menu logic.
    /// This class implements <see cref="IConsoleService"/> and <see cref="IDisposable"/>.
    /// </summary>
    public abstract class BaseConsoleService : IConsoleService, IDisposable
    {
        ///<summary>
        /// Logging service for recording operations and errors.
        ///</summary>
        protected readonly ILoggingService _loggingService;

        /// <summary>
        /// The primary Key Vault service used for secret operations.
        /// </summary>
        protected IKeyVaultService _keyVaultService;

        /// <summary>
        /// Current Key Vault URL in use.
        /// </summary>
        protected string _keyVaultUrl;

        /// <summary>
        /// The path to the local configuration file.
        /// </summary>
        protected readonly string _configPath;

        /// <summary>
        /// Indicates whether the header has already been displayed.
        /// </summary>
        protected bool _headerShown = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseConsoleService"/> class.
        /// </summary>
        /// <param name="keyVaultService">An initialized <see cref="IKeyVaultService"/> for interacting with Azure Key Vault.</param>
        /// <param name="loggingService">The logging service <see cref="ILoggingService"/> for recording operations and errors.</param>
        /// <param name="keyVaultUrl">The Key Vault URL currently in use by the service.</param>
        /// <param name="configPath">The path to the local configuration file used by the service.</param>
        protected BaseConsoleService(
            IKeyVaultService keyVaultService,
            ILoggingService loggingService,
            string keyVaultUrl,
            string configPath)
        {
            _keyVaultService = keyVaultService;
            _loggingService = loggingService;
            _keyVaultUrl = keyVaultUrl;
            _configPath = configPath;
        }

        /// <summary>
        /// When implemented by a derived class, runs the main execution flow of the console service.
        /// </summary>
        public abstract Task RunAsync();

        /// <summary>
        /// Runs the interactive console loop for menu-based usage.
        /// Commonly shared between <see cref="ConsoleService"/> and <see cref="CommandLineConsoleService"/>.
        /// </summary>
        protected virtual async Task RunInteractiveConsole()
        {
            ShowHeader();

            while (true)
            {
                try
                {
                    ShowMenu();
                    var choice = Console.ReadLine()?.Trim().ToLower();

                    if (int.TryParse(choice, out int choiceNumber))
                    {
                        if (Enum.IsDefined(typeof(MenuOption), choiceNumber))
                        {
                            var selectedOption = (MenuOption)choiceNumber;
                            await RunMenuOption(selectedOption);

                            // Some options exit the application
                            if (selectedOption == MenuOption.ExitApplication ||
                                selectedOption == MenuOption.ExitApplicationAndClear)
                            {
                                return;
                            }
                        }
                        else if (choice == "clear")
                        {
                            Console.Clear();
                        }
                        else
                        {
                            Helpers.PrintError("Invalid choice");
                        }
                    }
                    else
                    {
                        Helpers.PrintError("Invalid choice");
                    }
                }
                catch (Exception ex)
                {
                    Helpers.HandleError(ex);
                    _loggingService.LogError(ex.Message);
                }
                finally
                {
                    Helpers.ResetConsole();
                }
            }
        }

        /// <summary>
        /// Invokes the appropriate action based on the selected menu option.
        /// </summary>
        /// <param name="selectedOption">The chosen <see cref="MenuOption"/>.</param>
        protected virtual async Task RunMenuOption(MenuOption selectedOption)
        {
            switch (selectedOption)
            {
                case MenuOption.AddPassword:
                    await AddPassword();
                    break;
                case MenuOption.DeletePassword:
                    await DeletePassword();
                    break;
                case MenuOption.UpdatePassword:
                    await UpdatePassword();
                    break;
                case MenuOption.ListPasswords:
                    await ListPasswords();
                    break;
                case MenuOption.FetchPassword:
                    await ShowPassword();
                    break;
                case MenuOption.RecoverPassword:
                    await RestorePassword();
                    break;
                case MenuOption.PurgePassword:
                    await PurgePassword();
                    break;
                case MenuOption.ListDeletedPasswords:
                    await ListDeletedPasswords();
                    break;
                case MenuOption.ChangeKeyvaultUrl:
                    await ChangeKeyVaultUrl();
                    break;
                case MenuOption.ExitApplication:
                    // Exit, no cleanup
                    break;
                case MenuOption.ExitApplicationAndClear:
                    // Exit and cleanup configuration
                    await CleanupAndExit();
                    break;
                default:
                    Helpers.PrintError("Invalid choice");
                    break;
            }
        }

        /// <summary>
        /// Displays the application header once per session, showing Key Vault info.
        /// </summary>
        protected virtual void ShowHeader()
        {
            if (!_headerShown)
            {
                Console.WriteLine($"{Properties.ColorSuccess}\n=== AZURE KEYVAULT PASSWORD MANAGEMENT ==={Properties.ColorReset}\n");
                Console.WriteLine($"Key Vault URL: {_keyVaultUrl}");
                Console.WriteLine($"Authentication type: {(_keyVaultService.IsUsingAadApp ? "Azure AD application" : "Automatic detection")}\n");
                _headerShown = true;
            }
        }

        /// <summary>
        /// Prints the menu of available operations to the console.
        /// </summary>
        protected virtual void ShowMenu()
        {
            Console.WriteLine("1. Add password");
            Console.WriteLine("2. Delete password");
            Console.WriteLine("3. Update password");
            Console.WriteLine("4. List passwords");
            Console.WriteLine("5. Show password");
            Console.WriteLine("6. Recover password");
            Console.WriteLine("7. Permanently purge password");
            Console.WriteLine("8. List deleted passwords");
            Console.WriteLine("9. Change Key Vault URL");
            Console.WriteLine("10. Exit application");
            Console.WriteLine("11. Exit and clear configuration");
            Console.Write("\nChoice: ");
        }

        /// <summary>
        /// Prompts the user for confirmation and, if confirmed, removes local config and environment registration.
        /// </summary>
        protected virtual async Task CleanupAndExit()
        {
            Console.WriteLine("\nDo you want to remove all local configuration files?");
            Console.WriteLine($"{(int)ConfirmationOption.Yes}. Yes");
            Console.WriteLine($"{(int)ConfirmationOption.No}. No");
            Console.Write("Your choice: ");
            var choice = Console.ReadLine();

            if (int.TryParse(choice, out int choiceNumber))
            {
                if (Enum.IsDefined(typeof(ConfirmationOption), choiceNumber))
                {
                    var selectedOption = (ConfirmationOption)choiceNumber;
                    if (selectedOption == ConfirmationOption.Yes)
                    {
                        try
                        {
                            if (File.Exists(_configPath))
                            {
                                File.Delete(_configPath);
                                Helpers.PrintSuccess("Configuration file removed");
                            }
                            // Remove encryption keys
                            SecurityHelper.CleanupEncryptionKeys();

                            // Unregister environment path
                            UnregisterAppPath();

                            Helpers.PrintSuccess("All local data has been removed");
                        }
                        catch (Exception ex)
                        {
                            Helpers.PrintError($"Failed to remove local data: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Helpers.PrintError("Invalid choice.");
                }
            }
            else
            {
                Helpers.PrintError("Invalid choice.");
            }
        }

        #region Password Operations (shared)
        /// <summary>
        /// Prompts the user to create and add a new password (secret) to Key Vault.
        /// </summary>
        protected virtual async Task AddPassword()
        {
            Console.Write("Password name: ");
            var name = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                Helpers.PrintError("Name cannot be empty");
                return;
            }
            if (name.Length > 127)
            {
                Helpers.PrintError("Name cannot exceed 127 characters");
                return;
            }
            if (await _keyVaultService.SecretExistsAsync(name))
            {
                Helpers.PrintError("A secret with this name already exists!");
                return;
            }

            Console.Write("Password: ");
            var password = Helpers.ReadPassword();
            try
            {
                await Helpers.ExecuteOperationAsync(
                    async () => await _keyVaultService.AddSecretAsync(name, password),
                    "Adding password");
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                Helpers.PrintError("A secret with this name already exists!");
                return;
            }
            Helpers.PrintSuccess($"Password '{name}' was successfully added!");
            _loggingService.LogOperation("Add", name);
        }

        /// <summary>
        /// Prompts the user to delete an existing password (secret) in Key Vault.
        /// </summary>
        protected async Task DeletePassword()
        {
            Console.Write("Name of the password to delete: ");
            var name = Console.ReadLine()?.Trim();

            await Helpers.ExecuteOperationAsync(
                async () => await _keyVaultService.DeleteSecretAsync(name),
                "Deleting password");

            Helpers.PrintSuccess($"Password '{name}' has been deleted!");
            _loggingService.LogOperation("Delete", name);
        }

        /// <summary>
        /// Prompts the user to update an existing password.
        /// </summary>
        protected async Task UpdatePassword()
        {
            Console.Write("Name of the password to update: ");
            var name = Console.ReadLine()?.Trim();

            Console.Write("New password: ");
            var password = Helpers.ReadPassword();

            await Helpers.ExecuteOperationAsync(
                async () => await _keyVaultService.UpdateSecretAsync(name, password),
                "Updating password");

            Helpers.PrintSuccess($"Password '{name}' was updated!");
            _loggingService.LogOperation("Update", name);
        }

        /// <summary>
        /// Prompts the user to fetch an existing password from Key Vault and displays it.
        /// </summary>
        protected async Task ShowPassword()
        {
            Console.Write("Name of the password to fetch: ");
            var name = Console.ReadLine()?.Trim();

            await Helpers.ExecuteOperationAsync(async () =>
            {
                var password = await _keyVaultService.GetSecretAsync(name);
                Console.WriteLine($"\n{Properties.ColorSuccess}Password '{name}': {password}{Properties.ColorReset}");
            }, "Fetching password");

            _loggingService.LogOperation("Show", name);
        }
        #endregion

        #region Listing Operations (shared)
        /// <summary>
        /// Lists all (or filtered) existing passwords in Key Vault.
        /// </summary>
        protected virtual async Task ListPasswords()
        {
            Console.Write("Search term (press Enter for all): ");
            var searchTerm = Console.ReadLine()?.Trim();

            await Helpers.ExecuteOperationAsync(async () =>
            {
                var results = await _keyVaultService.ListSecretsAsync(searchTerm);
                if (results.Count == 0) throw new Exception("No results found");

                Console.WriteLine($"\n{Properties.ColorSuccess}Found secrets ({results.Count}):{Properties.ColorReset}\n");
                results.ForEach(secretName => Console.WriteLine($"- {secretName}"));
            }, "Listing passwords");
        }

        /// <summary>
        /// Lists all deleted (soft-deleted) passwords in Key Vault.
        /// </summary>
        protected virtual async Task ListDeletedPasswords()
        {
            await Helpers.ExecuteOperationAsync(async () =>
            {
                var results = await _keyVaultService.ListDeletedSecretsAsync();
                if (results.Count == 0) throw new Exception("No deleted passwords found");

                Console.WriteLine($"\n{Properties.ColorSuccess}Deleted passwords:{Properties.ColorReset}");
                results.ForEach(secret => Console.WriteLine($"- {secret.Name} (deleted on: {secret.DeletedOn:dd.MM.yyyy HH:mm})"));
            }, "Listing deleted passwords");
        }
        #endregion

        /// <summary>
        /// Helper method to unregister the application path. Can be overridden for testing.
        /// </summary>
        protected virtual void UnregisterAppPath()
        {
            var envService = new EnvironmentService();
            envService.UnregisterApplicationPath();
        }

        #region Deleted Secret Operations (shared)
        /// <summary>
        /// Prompts the user for a password name and restores the soft-deleted secret in Key Vault.
        /// </summary>
        protected virtual async Task RestorePassword()
        {
            Console.Write("Name of the password to restore: ");
            var name = Console.ReadLine()?.Trim();

            await Helpers.ExecuteOperationAsync(
                async () => await _keyVaultService.RestoreSecretAsync(name),
                "Restoring password");

            Helpers.PrintSuccess($"Password '{name}' restored successfully!");
            _loggingService.LogOperation("Restore", name);
        }

        /// <summary>
        /// Prompts the user for a password name and purges (permanently deletes) the secret from Key Vault.
        /// </summary>
        protected virtual async Task PurgePassword()
        {
            Console.Write("Name of the password to permanently purge: ");
            var name = Console.ReadLine()?.Trim();

            Console.Write("Are you sure? (yes/no): ");
            if (Console.ReadLine()?.Trim().ToLower() != "yes") return;

            await Helpers.ExecuteOperationAsync(
                async () => await _keyVaultService.PurgeSecretAsync(name),
                "Permanently purging password");

            Helpers.PrintSuccess($"Password '{name}' was permanently purged!");
            _loggingService.LogOperation("Purge", name);
        }
        #endregion

        #region Configuration (shared)
        /// <summary>
        /// Prompts the user for a new Key Vault URL and reinitializes the <see cref="_keyVaultService"/>.
        /// </summary>
        protected virtual async Task ChangeKeyVaultUrl()
        {
            Console.Write("New Key Vault URL: ");
            var newUrl = Console.ReadLine()?.Trim();

            if (!Uri.TryCreate(newUrl, UriKind.Absolute, out var uri) ||
                !uri.Host.EndsWith("vault.azure.net", StringComparison.OrdinalIgnoreCase))
            {
                Helpers.PrintError("Invalid Key Vault URL");
                return;
            }

            try
            {
                // Test new connection
                var settings = new KeyVaultSettings { KeyVaultUrl = newUrl };
                var tempService = new KeyVaultService(settings, _loggingService);
                await tempService.InitializeAsync();
                await tempService.ListSecretsAsync();

                // Reinitialize
                _keyVaultService = new KeyVaultService(settings, _loggingService);
                _keyVaultUrl = newUrl;

                // Save
                SaveConfiguration(_keyVaultService.IsUsingAadApp
                    ? $"{newUrl}|{_keyVaultService.TenantId}|{_keyVaultService.ClientId}|{_keyVaultService.ClientSecret}"
                    : newUrl);

                Helpers.PrintSuccess($"Key Vault URL changed successfully: {newUrl}");
                _headerShown = false;
                ShowHeader();
            }
            catch (Exception ex)
            {
                Helpers.PrintError($"Error changing URL: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves updated configuration data securely to the local config file.
        /// </summary>
        /// <param name="configData">The serialized config data to be stored.</param>
        protected virtual void SaveConfiguration(string configData)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath));
            SecurityHelper.SaveEncryptedConfig(_configPath, configData);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            (_loggingService as IDisposable)?.Dispose();
            (_keyVaultService as IDisposable)?.Dispose();
        }
        #endregion
    }
}