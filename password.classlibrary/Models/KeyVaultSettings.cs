namespace password.classlibrary.Models
{
    /// <summary>
    /// Represents the configuration settings for connecting to Azure Key Vault.
    /// </summary>
    public class KeyVaultSettings
    {
        /// <summary>
        /// The URL of the Azure Key Vault instance.
        /// </summary>
        public string KeyVaultUrl { get; set; }

        /// <summary>
        /// The Tenant ID of the Azure Active Directory instance.
        /// Required when using an Azure AD application for authentication.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// The Client ID (Application ID) of the Azure AD application.
        /// Required when using an Azure AD application for authentication.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// The Client Secret of the Azure AD application.
        /// Required when using an Azure AD application with a secret for authentication.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// The path to the client certificate file.
        /// Required when using a certificate for Azure AD application authentication.
        /// </summary>
        public string CertificatePath { get; set; }

        /// <summary>
        /// The password for the client certificate file.
        /// Required if the certificate file is password protected.
        /// </summary>
        public string CertificatePassword { get; set; }

        /// <summary>
        /// A flag indicating whether to use Managed Identity for authentication.
        /// When true, the application will attempt to authenticate using the configured Managed Identity.
        /// </summary>
        public bool UseManagedIdentity { get; set; }
    }
}