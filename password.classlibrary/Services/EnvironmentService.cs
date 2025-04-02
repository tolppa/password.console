using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using password.classlibrary.Interfaces;
using password.classlibrary.Utils;

namespace password.classlibrary.Services
{
    /// <summary>
    /// Provides functionality to register or unregister this application in the operating system's environment
    /// so that it can run from any directory without specifying a full path. Supports both Windows and Unix systems.
    /// Implements the <see cref="IEnvironmentService"/> interface.
    /// </summary>
    public class EnvironmentService : IEnvironmentService
    {
        /// <summary>
        /// The application name used when registering environment variables or aliases.
        /// </summary>
        private readonly string _appName;

        /// <summary>
        /// The directory path where the application's executable resides.
        /// </summary>
        private readonly string _appPath;

        /// <summary>
        /// The full path of the application's executable (e.g., .dll or .exe).
        /// </summary>
        private readonly string _executablePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentService"/> class.
        /// </summary>
        /// <param name="applicationName">The name of the application; defaults to "password.console".</param>
        public EnvironmentService(string applicationName = "password.console")
        {
            _appName = applicationName;
            _executablePath = Assembly.GetExecutingAssembly().Location;
            _appPath = Path.GetDirectoryName(_executablePath);
        }

        /// <summary>
        /// Registers the application so that it can be run from any directory.
        /// Optionally prompts the user for confirmation in an interactive console.
        /// </summary>
        /// <param name="interactive">
        /// If true, prompts the user to confirm the registration. If false, registers silently.
        /// </param>
        public void RegisterApplicationPath(bool interactive = true)
        {
            if (interactive)
            {
                Console.WriteLine("\nDo you want to register the application for easier use?");
                Console.WriteLine("1. Yes (can be run from any directory)");
                Console.WriteLine("2. No");
                Console.Write("Your choice: ");
                var choice = Console.ReadLine();

                if (choice != "1") return;
            }

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    RegisterForWindows();
                }
                else
                {
                    RegisterForUnix();
                }

                Console.WriteLine("\nRegistration successful!");
                PrintUsageInstructions();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nRegistration failed: {ex.Message}");
                PrintManualInstructions();
            }
        }

        /// <summary>
        /// Unregisters the application by removing environment variables, path entries,
        /// and other references previously created by <see cref="RegisterApplicationPath"/>.
        /// </summary>
        public void UnregisterApplicationPath()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Remove user-level environment variable
                    Environment.SetEnvironmentVariable(_appName, null, EnvironmentVariableTarget.User);

                    // Remove the application directory from the PATH
                    RemoveFromWindowsPath();

                    Console.WriteLine($"Windows environment variable '{_appName}' removed");
                }
                else
                {
                    RemoveUnixRegistration();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unregistration failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks whether the application is already registered in the OS environment
        /// (i.e., if the path or alias for this application is set).
        /// </summary>
        /// <returns>True if the application is registered, otherwise false.</returns>
        public bool IsRegistered()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var path = Environment.GetEnvironmentVariable(_appName, EnvironmentVariableTarget.User);
                var pathVar = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);

                return (!string.IsNullOrEmpty(path) && path == _appPath)
                       || (!string.IsNullOrEmpty(pathVar) && pathVar.Contains(_appPath, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return CheckUnixRegistration();
            }
        }

        #region Windows-specific

        /// <summary>
        /// Registers this application on Windows by adding the application's directory to
        /// the user PATH and creating an environment variable for it in the registry.
        /// </summary>
        /// <exception cref="Exception">
        /// Thrown if there are registry or path update errors, such as permission issues.
        /// </exception>
        private void RegisterForWindows()
        {
            // 1. Add the application directory to the PATH variable
            var path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
            if (!path.Contains(_appPath, StringComparison.OrdinalIgnoreCase))
            {
                path = string.IsNullOrEmpty(path) ? _appPath : $"{path};{_appPath}";
                Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.User);
            }

            // 2. Write to the registry (User Environment key)
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Environment", writable: true))
                {
                    key.SetValue(_appName, _appPath);
                    key.SetValue("PATH", path, Microsoft.Win32.RegistryValueKind.ExpandString);
                }

                // Broadcast the change to the system so that new shells pick up the changes
                NativeMethods.SendMessageTimeout(
                    NativeMethods.HWND_BROADCAST,
                    NativeMethods.WM_SETTINGCHANGE,
                    UIntPtr.Zero,
                    "Environment",
                    NativeMethods.SMTO_ABORTIFHUNG,
                    5000,
                    out _);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Writing to registry failed: {ex.Message}");
                throw;
            }

            Console.WriteLine($"\nApplication registered successfully! You can use the command:");
            Console.WriteLine($"{_appName}");
            Console.WriteLine("\nNOTE: You may need to open a new command prompt for changes to take effect.");
        }

        /// <summary>
        /// Removes the application's directory from the user's PATH variable if present.
        /// </summary>
        private void RemoveFromWindowsPath()
        {
            var path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
            if (!string.IsNullOrEmpty(path) && path.Contains(_appPath, StringComparison.OrdinalIgnoreCase))
            {
                var newPath = string.Join(";", path.Split(';')
                    .Where(p => !p.Equals(_appPath, StringComparison.OrdinalIgnoreCase)));
                Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
            }
        }

        #endregion

        #region Unix-specific (macOS/Linux)

        /// <summary>
        /// Registers this application on Unix (macOS/Linux) by adding alias and PATH export lines
        /// to the current user's shell configuration file (e.g., .bashrc, .zshrc, .config/fish/config.fish).
        /// </summary>
        /// <exception cref="IOException">Thrown if reading/writing the shell config file fails.</exception>
        private void RegisterForUnix()
        {
            string shellConfigFile = GetShellConfigFile();
            string aliasLine = $"alias {_appName.ToLowerInvariant()}=\"{_executablePath}\"";
            string pathLine = $"export PATH=\"$PATH:{_appPath}\"";

            // Create a backup of the shell config file if it exists
            if (File.Exists(shellConfigFile))
            {
                File.Copy(shellConfigFile, $"{shellConfigFile}.bak", true);
            }

            // Load existing config (if any)
            var configContent = File.Exists(shellConfigFile)
                ? File.ReadAllLines(shellConfigFile).ToList()
                : new List<string>();

            // Remove any old registration lines for this app
            configContent.RemoveAll(line =>
                line.Contains(_appName, StringComparison.OrdinalIgnoreCase) ||
                line.Contains(_executablePath, StringComparison.OrdinalIgnoreCase) ||
                line.Contains(_appPath, StringComparison.OrdinalIgnoreCase));

            // Add new lines
            configContent.Add($"# {_appName} configuration");
            configContent.Add(aliasLine);
            configContent.Add(pathLine);
            configContent.Add("");

            // Write back the updated config
            File.WriteAllLines(shellConfigFile, configContent);

            // Make the application file executable
            try
            {
                Process.Start("chmod", $"+x \"{_executablePath}\"");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not set execute permissions: {ex.Message}");
            }
        }

        /// <summary>
        /// Removes lines associated with this application from the user's shell config file.
        /// </summary>
        private void RemoveUnixRegistration()
        {
            string shellConfigFile = GetShellConfigFile();
            if (!File.Exists(shellConfigFile)) return;

            var lines = File.ReadAllLines(shellConfigFile)
                .Where(line =>
                    !line.Contains($"# {_appName}", StringComparison.OrdinalIgnoreCase) &&
                    !line.Contains(_executablePath, StringComparison.OrdinalIgnoreCase) &&
                    !line.Contains(_appPath, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            File.WriteAllLines(shellConfigFile, lines);
        }

        /// <summary>
        /// Checks whether the application is registered by looking for the
        /// alias/path lines in the user's shell config file.
        /// </summary>
        /// <returns>True if lines referencing this application's path or executable are found.</returns>
        private bool CheckUnixRegistration()
        {
            string shellConfigFile = GetShellConfigFile();
            if (!File.Exists(shellConfigFile)) return false;

            var content = File.ReadAllText(shellConfigFile);
            return content.Contains(_executablePath, StringComparison.OrdinalIgnoreCase) || content.Contains(_appPath, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines which shell configuration file (e.g. ~/.bashrc, ~/.zshrc, etc.) to modify based on the current shell.
        /// </summary>
        /// <returns>The full file path to the appropriate shell config file.</returns>
        private static string GetShellConfigFile()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string shell = Environment.GetEnvironmentVariable("SHELL") ?? "/bin/bash";

            if (shell.EndsWith("zsh", StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(home, ".zshrc");
            }
            else if (shell.EndsWith("fish", StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(home, ".config/fish/config.fish");
            }
            else
            {
                // Default to bash
                return Path.Combine(home, ".bashrc");
            }
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Prints instructions advising the user on how to run the application after registration
        /// (e.g., open a new terminal, type the application name).
        /// </summary>
        private void PrintUsageInstructions()
        {
            Console.WriteLine("\nUsage instructions:");
            Console.WriteLine("1. Close all command prompts");
            Console.WriteLine("2. Open a NEW command prompt (CMD or PowerShell), or new terminal on Unix");
            Console.WriteLine($"3. Type: {_appName}");
            Console.WriteLine("\nIf the command is still not recognized, try:");
            Console.WriteLine($"- Typing the full path: \"{_executablePath}\"");
            Console.WriteLine("- Restarting the computer");
        }

        /// <summary>
        /// Prints manual registration instructions for users who encounter errors or need to register the application manually.
        /// </summary>
        private void PrintManualInstructions()
        {
            Console.WriteLine("\nYou can register manually:");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("1. Open System Properties -> Environment Variables");
                Console.WriteLine($"2. Add user variable named: {_appName}");
                Console.WriteLine($"   Value: {_appPath}");
                Console.WriteLine($"3. Add to PATH variable: {_appPath}");
            }
            else
            {
                string configFile = GetShellConfigFile();
                Console.WriteLine($"1. Edit file: {configFile}");
                Console.WriteLine($"2. Add to the end:");
                Console.WriteLine($"   alias {_appName.ToLowerInvariant()}=\"{_executablePath}\"");
                Console.WriteLine($"   export PATH=\"$PATH:{_appPath}\"");
                Console.WriteLine("3. Run: chmod +x \"{_executablePath}\"");
                Console.WriteLine($"4. Reload: source {configFile}");
            }
        }

        #endregion
    }
}