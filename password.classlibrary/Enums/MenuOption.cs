namespace password.classlibrary.Enums
{
    /// <summary>
    /// Enumerates the available options in the console application's interactive menu.
    /// </summary>
    public enum MenuOption
    {
        /// <summary>
        /// Option to add a new password (secret) to the Azure Key Vault.
        /// </summary>
        AddPassword = 1,
        /// <summary>
        /// Option to delete an existing password (secret) from the Azure Key Vault.
        /// </summary>
        DeletePassword = 2,
        /// <summary>
        /// Option to update the value of an existing password (secret) in the Azure Key Vault.
        /// </summary>
        UpdatePassword = 3,
        /// <summary>
        /// Option to list all the passwords (secrets) currently stored in the Azure Key Vault.
        /// </summary>
        ListPasswords = 4,
        /// <summary>
        /// Option to fetch and display the value of a specific password (secret) from the Azure Key Vault.
        /// </summary>
        FetchPassword = 5,
        /// <summary>
        /// Option to recover a soft-deleted password (secret) from the Azure Key Vault.
        /// </summary>
        RecoverPassword = 6,
        /// <summary>
        /// Option to permanently delete (purge) a soft-deleted password (secret) from the Azure Key Vault.
        /// </summary>
        PurgePassword = 7,
        /// <summary>
        /// Option to list all the passwords (secrets) that have been deleted (soft-deleted) in the Azure Key Vault.
        /// </summary>
        ListDeletedPasswords = 8,
        /// <summary>
        /// Option to change the URL of the Azure Key Vault instance that the application is currently connected to.
        /// </summary>
        ChangeKeyvaultUrl = 9,
        /// <summary>
        /// Option to exit the console application without performing any cleanup.
        /// </summary>
        ExitApplication = 10,
        /// <summary>
        /// Option to exit the console application and clear any local configuration data.
        /// </summary>
        ExitApplicationAndClear = 11,
    }
}