using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
#if NETFRAMEWORK
using System.Net;
#endif

namespace password.classlibrary.Utils
{
    public static class SecurityHelper
    {
        internal static byte[] _entropy;
        private static readonly string EntropyFileName = ".passwordapp_entropy"; // Fallback for Linux/macOS
        private const string EntropyCredentialName = "PasswordAppEntropy"; // Used for Windows Credential Manager

        /// <summary>
        /// Cleans up any encryption keys or entropy files that might exist.
        /// This is intended for security purposes, such as after uninstalling the application.
        /// </summary>
        public static void CleanupEncryptionKeys()
        {
            try
            {
                // Windows file path
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var entropyFile = Path.Combine(localAppData, EntropyFileName);
                Console.WriteLine($"Deleting Windows file: {entropyFile}"); // Debug log
                if (File.Exists(entropyFile))
                {
                    File.Delete(entropyFile);
                }

                // Unix file path
                var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var unixEntropyFile = Path.Combine(homePath, EntropyFileName);
                Console.WriteLine($"Deleting Unix file: {unixEntropyFile}"); // Debug log
                if (File.Exists(unixEntropyFile))
                {
                    File.Delete(unixEntropyFile);
                }
            }
            catch (Exception ex)
            {
                // Log exceptions for debugging
                Console.WriteLine($"Cleanup error: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads a password securely from the console, masking the input.
        /// </summary>
        /// <param name="prompt">The prompt to display to the user.</param>
        /// <returns>The password entered by the user as a string.</returns>
        public static string ReadSecurePassword(string prompt = "Password")
        {
            Console.Write($"{prompt}: ");
            using var secureString = ReadSecurePasswordInternal();
            return ConvertToUnsecureString(secureString);
        }

        /// <summary>
        /// Saves encrypted content to the specified file path.
        /// Uses Windows Data Protection API (DPAPI) on Windows and AES encryption on other platforms.
        /// </summary>
        /// <param name="path">The file path to save the encrypted content to.</param>
        /// <param name="content">The content to encrypt and save.</param>
        public static void SaveEncryptedConfig(string path, string content)
        {
            byte[] contentBytes = Encoding.UTF8.GetBytes(content);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var encrypted = ProtectedData.Protect(contentBytes, Entropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(path, encrypted);
            }
            else
            {
                using Aes aesAlg = Aes.Create();
                aesAlg.Key = Entropy;
                aesAlg.GenerateIV();
                var iv = aesAlg.IV;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, iv);

                using MemoryStream msEncrypt = new();
                using (CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    csEncrypt.Write(contentBytes, 0, contentBytes.Length);
                    csEncrypt.FlushFinalBlock();
                }
                File.WriteAllBytes(path, Combine(iv, msEncrypt.ToArray()));
            }
        }

        /// <summary>
        /// Loads and decrypts content from the specified file path.
        /// Uses Windows Data Protection API (DPAPI) on Windows and AES decryption on other platforms.
        /// </summary>
        /// <param name="path">The file path to load the encrypted content from.</param>
        /// <returns>The decrypted content as a string, or null if an error occurs or the file does not exist.</returns>
        public static string LoadEncryptedConfig(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            byte[] encryptedBytes = File.ReadAllBytes(path);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var decrypted = ProtectedData.Unprotect(encryptedBytes, Entropy, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(decrypted);
                }
                catch
                {
                    return null; // Invalid encrypted data
                }
            }
            else
            {
                using Aes aesAlg = Aes.Create();
                var ivLength = aesAlg.IV.Length;
                if (encryptedBytes.Length <= ivLength)
                {
                    return null; // Invalid encrypted data
                }
                var iv = new byte[ivLength];
                Buffer.BlockCopy(encryptedBytes, 0, iv, 0, ivLength);
                var encryptedContent = new byte[encryptedBytes.Length - ivLength];
                Buffer.BlockCopy(encryptedBytes, ivLength, encryptedContent, 0, encryptedContent.Length);

                aesAlg.Key = Entropy;
                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, iv);

                using MemoryStream msDecrypt = new MemoryStream(encryptedContent);
                using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
                using StreamReader srDecrypt = new(csDecrypt);
                return srDecrypt.ReadToEnd();
            }
        }

        /// <summary>
        /// Gets the entropy used for encryption. Initializes it if it hasn't been already.
        /// </summary>
        internal static byte[] Entropy
        {
            get
            {
                if (_entropy == null)
                {
                    _entropy = InitializeSecureEntropy();
                }
                return _entropy;
            }
        }

        /// <summary>
        /// Converts a SecureString to an unsecure string.
        /// Be cautious when using this method as it exposes the password in plain text in memory.
        /// </summary>
        /// <param name="securePassword">The SecureString to convert.</param>
        /// <returns>The unsecure string representation of the SecureString.</returns>
        internal static string ConvertToUnsecureString(SecureString securePassword)
        {
            if (securePassword == null)
                return string.Empty;

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                if (unmanagedString != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
                }
            }
        }

        /// <summary>
        /// Combines two byte arrays into a single byte array.
        /// </summary>
        /// <param name="a">The first byte array.</param>
        /// <param name="b">The second byte array.</param>
        /// <returns>A new byte array containing the combined bytes of the input arrays.</returns>
        internal static byte[] Combine(byte[] a, byte[] b)
        {
            var result = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, result, 0, a.Length);
            Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
            return result;
        }

        /// <summary>
        /// Reads password input securely from the console, masking the characters.
        /// </summary>
        /// <returns>A SecureString containing the password.</returns>
        private static SecureString ReadSecurePasswordInternal()
        {
            var securePassword = new SecureString();
            try
            {
                ConsoleKeyInfo keyInfo;
                do
                {
                    keyInfo = Console.ReadKey(true);

                    if (keyInfo.Key == ConsoleKey.Backspace && securePassword.Length > 0)
                    {
                        Console.Write("\b \b");
                        securePassword.RemoveAt(securePassword.Length - 1);
                    }
                    else if (!char.IsControl(keyInfo.KeyChar))
                    {
                        securePassword.AppendChar(keyInfo.KeyChar);
                        Console.Write("*");
                    }
                } while (keyInfo.Key != ConsoleKey.Enter);

                Console.WriteLine();
                return securePassword;
            }
            catch
            {
                securePassword.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Initializes the secure entropy used for encryption.
        /// It tries to load it from the Windows Credential Manager on Windows,
        /// then falls back to ProtectedData, then to a file on Unix-like systems,
        /// and finally generates a new random entropy.
        /// </summary>
        /// <returns>The initialized entropy as a byte array.</returns>
        private static byte[] InitializeSecureEntropy()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
#if NETFRAMEWORK
                try
                {
                    var credential = CredentialManager.ReadCredential(EntropyCredentialName);
                    if (credential != null)
                    {
                        return Encoding.UTF8.GetBytes(credential.Password);
                    }
                    else
                    {
                        var newEntropy = GenerateRandomEntropy();
                        var newCredential = new NetworkCredential("", Convert.ToBase64String(newEntropy));
                        CredentialManager.SaveCredential(EntropyCredentialName, newCredential, CredentialManager.PersistanceType.Local);
                        return newEntropy;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading/saving entropy from Windows Credential Manager: {ex.Message}");
                    return GetOrCreateProtectedDataEntropy();
                }
#else
                return GetOrCreateProtectedDataEntropy();
#endif
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // IMPORTANT: macOS Keychain and Linux Secret Service API integration require external libraries.
                // This implementation uses file-based storage as a fallback. Strongly consider
                // using external libraries such as 'keychain-sharp' for macOS and 'LibsecretSharp' for Linux.
                return GetOrCreateFileEntropy();
            }
            else
            {
                return GenerateRandomEntropy();
            }
        }

        /// <summary>
        /// Gets or creates entropy using the Windows Data Protection API (DPAPI).
        /// The entropy is stored in a file in the local application data folder.
        /// </summary>
        /// <returns>The entropy as a byte array.</returns>
        private static byte[] GetOrCreateProtectedDataEntropy()
        {
            try
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), EntropyFileName);
                if (File.Exists(path))
                {
                    var encryptedEntropy = File.ReadAllBytes(path);
                    return ProtectedData.Unprotect(encryptedEntropy, null, DataProtectionScope.CurrentUser);
                }
                else
                {
                    var newEntropy = GenerateRandomEntropy();
                    var encryptedNewEntropy = ProtectedData.Protect(newEntropy, null, DataProtectionScope.CurrentUser);
                    File.WriteAllBytes(path, encryptedNewEntropy);
                    return newEntropy;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading/saving ProtectedData entropy: {ex.Message}");
                return GenerateRandomEntropy();
            }
        }

        /// <summary>
        /// Gets or creates entropy from a file in the user's home directory.
        /// This is used as a fallback on Unix-like systems.
        /// </summary>
        /// <returns>The entropy as a byte array.</returns>
        private static byte[] GetOrCreateFileEntropy()
        {
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var entropyPath = Path.Combine(homePath, EntropyFileName);

            try
            {
                if (File.Exists(entropyPath))
                {
                    return File.ReadAllBytes(entropyPath);
                }
                else
                {
                    var newEntropy = GenerateRandomEntropy();
                    File.WriteAllBytes(entropyPath, newEntropy);
                    // IMPORTANT: Remember to manually set the file permissions (e.g., chmod 600 ~/.passwordapp_entropy)
                    // on macOS and Linux so that only the user has access to this file.
                    return newEntropy;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading/saving entropy from file: {ex.Message}");
                return GenerateRandomEntropy();
            }
        }

        /// <summary>
        /// Generates a cryptographically secure random byte array to be used as entropy.
        /// </summary>
        /// <param name="length">The length of the entropy to generate (default is 32 bytes).</param>
        /// <returns>A new random byte array.</returns>
        private static byte[] GenerateRandomEntropy(int length = 32)
        {
            using var rng = RandomNumberGenerator.Create();
            var entropy = new byte[length];
            rng.GetBytes(entropy);
            return entropy;
        }
    }
}