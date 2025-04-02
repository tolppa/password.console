using Azure.Security.KeyVault.Secrets;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace password.classlibrary.Interfaces
{
    /// <summary>
    /// Defines an interface for interacting with Azure Key Vault to manage secrets.
    /// </summary>
    public interface IKeyVaultService
    {
        /// <summary>
        /// Gets a value indicating whether the service is currently using Azure Active Directory (AAD) application authentication.
        /// </summary>
        bool IsUsingAadApp { get; }

        /// <summary>
        /// Gets the Tenant ID of the Azure Active Directory instance used for authentication.
        /// </summary>
        string TenantId { get; }

        /// <summary>
        /// Gets the Client ID (Application ID) of the Azure AD application used for authentication.
        /// </summary>
        string ClientId { get; }

        /// <summary>
        /// Gets the Client Secret of the Azure AD application used for authentication.
        /// </summary>
        string ClientSecret { get; }

        /// <summary>
        /// Initializes the Key Vault service, performing any necessary setup or authentication.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Adds a new secret to the Key Vault.
        /// </summary>
        /// <param name="name">The name of the secret to add.</param>
        /// <param name="password">The value of the secret.</param>
        Task AddSecretAsync(string name, string password);

        /// <summary>
        /// Retrieves the value of a secret from the Key Vault.
        /// </summary>
        /// <param name="name">The name of the secret to retrieve.</param>
        /// <returns>The value of the secret.</returns>
        Task<string> GetSecretAsync(string name);

        /// <summary>
        /// Updates the value of an existing secret in the Key Vault.
        /// </summary>
        /// <param name="name">The name of the secret to update.</param>
        /// <param name="newPassword">The new value for the secret.</param>
        Task UpdateSecretAsync(string name, string newPassword);

        /// <summary>
        /// Deletes a secret from the Key Vault. The secret will be soft-deleted if soft delete is enabled on the vault.
        /// </summary>
        /// <param name="name">The name of the secret to delete.</param>
        Task DeleteSecretAsync(string name);

        /// <summary>
        /// Lists all secrets in the Key Vault, optionally filtering by a search term.
        /// </summary>
        /// <param name="searchTerm">An optional search term to filter the list of secrets by name.</param>
        /// <param name="ct">An optional <see cref="CancellationToken"/> to cancel the operation.</param>
        /// <returns>A list of secret names.</returns>
        Task<List<string>> ListSecretsAsync(string searchTerm = null, CancellationToken ct = default);

        /// <summary>
        /// Lists all deleted (soft-deleted) secrets in the Key Vault.
        /// </summary>
        /// <returns>A list of deleted secrets with their metadata.</returns>
        Task<List<DeletedSecret>> ListDeletedSecretsAsync();

        /// <summary>
        /// Restores a soft-deleted secret in the Key Vault.
        /// </summary>
        /// <param name="name">The name of the secret to restore.</param>
        Task RestoreSecretAsync(string name);

        /// <summary>
        /// Permanently deletes a secret from the Key Vault. This operation can only be performed on a soft-deleted secret and requires the "purge" permission.
        /// </summary>
        /// <param name="name">The name of the secret to purge.</param>
        Task PurgeSecretAsync(string name);

        /// <summary>
        /// Checks if a secret with the given name exists in the Key Vault (including soft-deleted secrets).
        /// </summary>
        /// <param name="name">The name of the secret to check for.</param>
        /// <returns><c>true</c> if a secret with the given name exists; otherwise, <c>false</c>.</returns>
        Task<bool> SecretExistsAsync(string name);
    }
}