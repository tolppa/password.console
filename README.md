# password.console
This console application is a cross-platform .NET solution for securely retrieving, storing, and managing passwords in Azure Key Vault. Instead of hard-coding secrets in code or local files, the app fetches them from Key Vault using Azure Identity, ensuring passwords are never stored in plain text. Any local configuration data (such as Vault URLs or credential settings) is encrypted using DPAPI on Windows or AES on macOS/Linux.

NOTE
On Windows, the application benefits from stronger security features (e.g., DPAPI-based encryption).
On macOS and Linux, robust security depends on careful configuration—particularly ensuring that your Key Vault access policies are correct and that locally encrypted files (for example, “.passwordapp_entropy”) have restricted permissions (e.g., chmod 600 ~/.passwordapp_entropy) so only authorized users can read them.

# Key Benefits
• Eliminates the need to hard-code passwords

• Protects local configuration data with built-in encryption

• Runs on Windows, Linux, and macOS

• Supports multiple authentication options (managed identity, certificate, client secret, interactive)

• Allows centralized key rotation in Azure Key Vault, helping keep credentials up to date and reducing overall risk

• Reduces overall security risks by centralizing secrets in Azure Key Vault

WARNING
This version has primarily been tested on Windows. While macOS and Linux are supported, always follow best practices and confirm configurations for those platforms.

# Usage Instructions (PowerShell Example)
Make sure the application is in your PATH or run it locally with “.\”.
To retrieve and immediately print a secret named “example” from Key Vault:

$secret = keyvault -get "example" | Write-Host  

Alternatively, to store the value in a variable for further processing:

$secret = keyvault -get "example"  
Write-Host $secret  

Always ensure Azure Key Vault is correctly configured (e.g., access policies) and that any locally stored encryption keys or configuration files are secure (for example, using chmod 600 on Linux/macOS).
 
# Instructions for Centrally Blocking Access

1. Identify the Identity Used by the Application
2. Remove Permission from Key Vault
3. Remove or Recreate Authorization in Microsoft Entra (Azure AD)

More detailed instructions have intentionally been omitted. If you are unsure how to configure Microsoft Entra ID / Azure Key Vault, please consult your system administrators or maintainers. This helps minimize the likelihood of misconfiguration.
   
# Removing Local Credential Files

 If the application stores local encryption keys or configuration files (e.g., “.keyvault_entropy,” “.passwordapp_entropy”), remove them to ensure no residual secrets remain:

On Linux/macOS:

rm ~/.keyvault_entropy

On Windows:

Del $env:USERPROFILE\.keyvault_entropy  

The application supports a built-in cleanup. Run it to remove any remaining local credentials.

By following these steps, you can securely manage secrets in Azure Key Vault, block or revoke access if necessary, and remove local credential files to reduce security risks across all supported platforms.
