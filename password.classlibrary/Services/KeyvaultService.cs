using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using password.classlibrary.Extensions;
using password.classlibrary.Interfaces;
using password.classlibrary.Models;
using password.classlibrary.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace password.classlibrary.Services
{
    /// <summary>
    /// Provides methods to interact with Azure Key Vault for creating, reading, updating, and deleting secrets.
    /// Also handles various Key Vault authentication strategies (managed identity, certificates, client secrets).
    /// Implements the <see cref="IKeyVaultService"/> and <see cref="IDisposable"/> interfaces.
    /// </summary>
    public class KeyVaultService : IKeyVaultService, IDisposable
    {
        /// <summary>
        /// The client used to perform operations on Azure Key Vault secrets.
        /// </summary>
        internal readonly SecretClient _secretClient;

        /// <summary>
        /// The Azure credential used to authenticate requests to Key Vault.
        /// </summary>
        internal readonly TokenCredential _credential;

        /// <summary>
        /// The logging service used for recording operations and errors.
        /// </summary>
        private readonly ILoggingService _loggingService;

        /// <summary>
        /// Maintains a set of user permissions for various operations (e.g., Get, Set, etc.).
        /// </summary>
        private readonly Dictionary<string, bool> _userPermissions = new Dictionary<string, bool>();

        /// <summary>
        /// Indicates whether <see cref="InitializeAsync"/> has been called successfully.
        /// </summary>
        private bool _initialized = false;

        /// <summary>
        /// Indicates if Azure AD application credentials are being used (as opposed to managed identity or interactive).
        /// </summary>
        public bool IsUsingAadApp { get; }

        /// <summary>
        /// The Azure AD tenant ID, used if <see cref="IsUsingAadApp"/> is true.
        /// </summary>
        public string TenantId { get; }

        /// <summary>
        /// The Azure AD client (application) ID, used if <see cref="IsUsingAadApp"/> is true.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// The client secret for the Azure AD application, if applicable.
        /// </summary>
        public string ClientSecret { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultService"/> class.
        /// Chooses the appropriate credential based on the provided <see cref="KeyVaultSettings"/>.
        /// </summary>
        /// <param name="settings">The Key Vault configuration settings.</param>
        /// <param name="loggingService">The logging service to record operations and errors.</param>
        /// <exception cref="ArgumentException">Thrown if the Key Vault URL is missing or invalid.</exception>
        /// <exception cref="InvalidOperationException">Thrown if no valid credentials are found or certificate loading fails.</exception>
        public KeyVaultService(KeyVaultSettings settings, ILoggingService loggingService)
        {
            ValidateSettings(settings);
            _loggingService = loggingService;

            var vaultUri = new Uri(settings.KeyVaultUrl);

            if (settings.UseManagedIdentity)
            {
                _credential = new DefaultAzureCredential();
                IsUsingAadApp = false;
            }
            else if (!string.IsNullOrEmpty(settings.CertificatePath))
            {
                var certificate = LoadCertificate(settings);
                _credential = new ClientCertificateCredential(
                    settings.TenantId,
                    settings.ClientId,
                    certificate);
                IsUsingAadApp = true;
                TenantId = settings.TenantId;
                ClientId = settings.ClientId;
            }
            else if (!string.IsNullOrEmpty(settings.ClientSecret))
            {
                _credential = new ClientSecretCredential(
                    settings.TenantId,
                    settings.ClientId,
                    settings.ClientSecret);
                IsUsingAadApp = true;
                TenantId = settings.TenantId;
                ClientId = settings.ClientId;
                ClientSecret = settings.ClientSecret;
            }
            else
            {
                // Fallback to combined interactive (browser/device code) authentication.
                _credential = new ChainedTokenCredential(
                    new InteractiveBrowserCredential(
                        new InteractiveBrowserCredentialOptions
                        {
                            TokenCachePersistenceOptions = new TokenCachePersistenceOptions
                            {
                                Name = "PasswordManagerCache",
                                UnsafeAllowUnencryptedStorage = false
                            }
                        }),
                    new DeviceCodeCredential(new DeviceCodeCredentialOptions
                    {
                        DeviceCodeCallback = async (context, cancellationToken) =>
                        {
                            _loggingService.LogOperation("Authentication", context.Message);
                            await Task.CompletedTask;
                        }
                    })
                );
                IsUsingAadApp = false;
            }

            _secretClient = new SecretClient(vaultUri, _credential);
        }

        #region Core Operations

        /// <inheritdoc/>
        public virtual async Task AddSecretAsync(string name, string password)
        {
            ValidateSecretName(name);
            await EnsureInitializedAsync("Set");

            var cleanedName = Helpers.CleanName(name);
            await ExecuteSecretOperationAsync(
                () => _secretClient.SetSecretAsync(cleanedName, password),
                "AddSecret",
                cleanedName
            );
        }

        /// <inheritdoc/>
        public virtual async Task<string> GetSecretAsync(string name)
        {
            ValidateSecretName(name);
            await EnsureInitializedAsync("Get");

            var cleanedName = Helpers.CleanName(name);
            try
            {
                // Added logging before the operation
                _loggingService.LogOperation("Fetching password", cleanedName);

                var secret = await _secretClient.GetSecretAsync(cleanedName);
                return secret.Value.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Added error logging
                _loggingService.LogError($"Secret '{name}' not found");
                throw new KeyNotFoundException($"Secret '{name}' not found");
            }
        }

        /// <inheritdoc/>
        public virtual async Task UpdateSecretAsync(string name, string newPassword)
        {
            ValidateSecretName(name);
            await EnsureInitializedAsync("Set");

            var cleanedName = Helpers.CleanName(name);
            await ExecuteSecretOperationAsync(
                () => _secretClient.SetSecretAsync(cleanedName, newPassword),
                "UpdateSecret",
                cleanedName
            );
        }

        /// <inheritdoc/>
        public virtual async Task DeleteSecretAsync(string name)
        {
            ValidateSecretName(name);
            await EnsureInitializedAsync("Delete");

            var cleanedName = Helpers.CleanName(name);
            await ExecuteSecretOperationAsync(
                () => _secretClient.StartDeleteSecretAsync(cleanedName),
                "DeleteSecret",
                cleanedName
            );
        }

        /// <inheritdoc/>
        public virtual async Task<List<string>> ListSecretsAsync(string searchTerm = null, CancellationToken ct = default)
        {
            await EnsureInitializedAsync("List");
            var results = new List<string>();

            await foreach (var page in _secretClient.GetPropertiesOfSecretsAsync(ct).AsPages())
            {
                foreach (var secretProps in page.Values)
                {
                    var decodedName = Helpers.DecodeName(secretProps.Name);
                    if (ShouldIncludeSecret(decodedName, searchTerm))
                    {
                        results.Add(decodedName);
                    }
                }
            }
            _loggingService.LogOperation("ListSecrets", $"Found {results.Count} secrets");
            return results;
        }

        /// <inheritdoc/>
        public virtual async Task<List<DeletedSecret>> ListDeletedSecretsAsync()
        {
            await EnsureInitializedAsync("List");
            var results = new List<DeletedSecret>();

            await foreach (var deletedSecret in _secretClient.GetDeletedSecretsAsync())
            {
                results.Add(deletedSecret);
            }
            _loggingService.LogOperation("ListDeletedSecrets", $"Found {results.Count} deleted secrets");
            return results;
        }

        /// <inheritdoc/>
        public virtual async Task RestoreSecretAsync(string name)
        {
            ValidateSecretName(name);
            await EnsureInitializedAsync("Recover");

            var cleanedName = Helpers.CleanName(name);
            try
            {
                var operation = await _secretClient.StartRecoverDeletedSecretAsync(cleanedName);
                await operation.WaitForCompletionAsync();
                _loggingService.LogOperation("RestoreSecret", cleanedName);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"RestoreSecret failed for '{cleanedName}': {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task PurgeSecretAsync(string name)
        {
            ValidateSecretName(name);
            await EnsureInitializedAsync("Purge");

            var cleanedName = Helpers.CleanName(name);
            try
            {
                await _secretClient.PurgeDeletedSecretAsync(cleanedName);
                _loggingService.LogOperation("PurgeSecret", cleanedName);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"PurgeSecret failed for '{cleanedName}': {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SecretExistsAsync(string name)
        {
            ValidateSecretName(name);
            await EnsureInitializedAsync("Get");

            var cleanedName = Helpers.CleanName(name);
            try
            {
                await _secretClient.GetSecretAsync(cleanedName);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return false;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"SecretExists check failed for '{cleanedName}': {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Initialization and Permissions

        /// <summary>
        /// Initializes the connection to Key Vault and verifies that basic permissions exist.
        /// Subsequent calls have no effect if already initialized.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Thrown if access is denied or insufficient permissions.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the Key Vault connection fails unexpectedly.</exception>
        public async Task InitializeAsync()
        {
            if (_initialized) return;

            try
            {
                await CheckPermissionsAsync();
                ShowPermissionStatus();
                _initialized = true;
            }
            catch (RequestFailedException ex) when (ex.Status == 403)
            {
                HandleForbiddenError(ex);
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Initialization failed: {ex}");
                throw new InvalidOperationException("Key Vault connection failed", ex);
            }
        }

        /// <summary>
        /// Performs a minimal API call to see if the current credential can list secret properties, and sets basic permissions.
        /// </summary>
        private async Task CheckPermissionsAsync()
        {
            /*
             * By default, this code attempts to list secret properties as a quick permission check.
             * Additional or more granular checks can be enabled if needed.
             */
            await _secretClient.GetPropertiesOfSecretsAsync()
                .AsPages()
                .GetAsyncEnumerator()
                .MoveNextAsync();

            _userPermissions["List"] = true;
            _userPermissions["Get"] = true;
            _userPermissions["Set"] = true;
            _userPermissions["Delete"] = true;
            _userPermissions["Recover"] = true;
            _userPermissions["Purge"] = true;
        }

        /// <summary>
        /// Handles 403 (Forbidden) errors by logging and, if necessary, throwing a descriptive exception.
        /// </summary>
        /// <param name="ex">The related exception.</param>
        private void HandleForbiddenError(Exception ex)
        {
            _loggingService.LogError($"Access denied: {ex.Message}");

            // If we cannot read or list secrets, inform the caller that read permissions are missing.
            if (!_userPermissions.GetValueOrDefault("Get", false) &&
                !_userPermissions.GetValueOrDefault("List", false))
            {
                throw new UnauthorizedAccessException(
                    "Insufficient read permissions. Requires 'Get' or 'List' permissions.");
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Ensures the service is initialized and the user has permission for the specified operation.
        /// </summary>
        /// <param name="operation">The operation name, such as "Get" or "Set".</param>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user lacks the required permission.</exception>
        private async Task EnsureInitializedAsync(string operation)
        {
            if (!_initialized) await InitializeAsync();

            if (!_userPermissions.GetValueOrDefault(operation, false))
            {
                throw new UnauthorizedAccessException(
                    $"Missing required permission for {operation} operation");
            }
        }

        /// <summary>
        /// Validates a given secret name for null/whitespace and length constraints.
        /// </summary>
        /// <param name="name">The secret name to validate.</param>
        /// <exception cref="ArgumentException">Thrown if the name is null/empty or exceeds 127 characters.</exception>
        private static void ValidateSecretName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Secret name cannot be empty");
            if (name.Length > 127)
                throw new ArgumentException("Secret name exceeds maximum length (127 chars)");
        }

        /// <summary>
        /// Executes a secret operation and logs any related exceptions or successes.
        /// </summary>
        /// <param name="operation">An asynchronous delegate to perform the secret operation.</param>
        /// <param name="operationName">A string describing the operation, e.g. "AddSecret".</param>
        /// <param name="secretName">The name of the secret on which the operation is performed.</param>
        /// <exception cref="RequestFailedException">Thrown if the secret operation fails with an Azure-related error.</exception>
        private async Task ExecuteSecretOperationAsync(Func<Task> operation, string operationName, string secretName)
        {
            try
            {
                await operation();
                _loggingService.LogOperation(operationName, secretName);
            }
            catch (RequestFailedException ex)
            {
                _loggingService.LogError($"{operationName} failed for '{secretName}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Determines whether a secret's name should be included based on an optional search term.
        /// </summary>
        /// <param name="secretName">The decoded secret name.</param>
        /// <param name="searchTerm">A filter term or null/empty for all.</param>
        /// <returns>True if the secret should be included, otherwise false.</returns>
        private static bool ShouldIncludeSecret(string secretName, string searchTerm)
        {
            return string.IsNullOrEmpty(searchTerm)
                   || secretName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Logs and displays any missing Key Vault permissions discovered during initialization.
        /// </summary>
        private void ShowPermissionStatus()
        {
            var missing = _userPermissions.Where(p => !p.Value).Select(p => p.Key).ToList();
            if (!missing.Any()) return;

            _loggingService.LogError($"Missing permissions: {string.Join(", ", missing)}");
            Console.WriteLine("\n[!] Limited functionality available");
            Console.WriteLine($"Missing permissions: {string.Join(", ", missing)}");
            Console.WriteLine("\nAvailable operations:");
            Console.WriteLine($"- Add/Update: {_userPermissions["Set"].ToYesNo()}");
            Console.WriteLine($"- Delete: {_userPermissions["Delete"].ToYesNo()}");
            Console.WriteLine($"- Restore: {_userPermissions["Recover"].ToYesNo()}");
            Console.WriteLine($"- Purge: {_userPermissions["Purge"].ToYesNo()}");
        }

        /// <summary>
        /// Loads an X.509 certificate (including its private key) from the details in <see cref="KeyVaultSettings"/>.
        /// </summary>
        /// <param name="settings">The settings object containing the certificate path and password.</param>
        /// <returns>An <see cref="X509Certificate2"/> instance with a private key.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the certificate does not contain a private key.</exception>
        private X509Certificate2 LoadCertificate(KeyVaultSettings settings)
        {
            try
            {
                var cert = new X509Certificate2(
                    settings.CertificatePath,
                    settings.CertificatePassword,
                    X509KeyStorageFlags.MachineKeySet |
                    X509KeyStorageFlags.PersistKeySet |
                    X509KeyStorageFlags.Exportable);

                if (!cert.HasPrivateKey)
                    throw new InvalidOperationException("Certificate does not contain a private key");

                return cert;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to load certificate: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Validates that <see cref="KeyVaultSettings.KeyVaultUrl"/> is provided and is a valid URI.
        /// </summary>
        /// <param name="settings">The settings to validate.</param>
        /// <exception cref="ArgumentException">Thrown if the URL is missing or invalid.</exception>
        private void ValidateSettings(KeyVaultSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.KeyVaultUrl))
                throw new ArgumentException("Key Vault URL is required");

            if (!Uri.TryCreate(settings.KeyVaultUrl, UriKind.Absolute, out _))
                throw new ArgumentException("Invalid Key Vault URL format");
        }

        #endregion

        #region Permission Testing (Optional)
        // Code for further granular testing if you want to test each permission individually.
        // These can be re-enabled if needed.
        #endregion

        public void Dispose()
        {
            (_credential as IDisposable)?.Dispose();
        }
    }
}