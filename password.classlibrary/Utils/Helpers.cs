using System.Text;
using System.Text.RegularExpressions;

namespace password.classlibrary.Utils
{
    /// <summary>
    /// Provides various helper methods for common tasks.
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Reads password input from the console, masking the characters.
        /// </summary>
        /// <returns>The password entered by the user as a string.</returns>
        public static string ReadPassword()
        {
            var password = new StringBuilder();
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Remove(password.Length - 1, 1);
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    password.Append(key.KeyChar);
                    Console.Write("*");
                }
            } while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return password.ToString();
        }

        /// <summary>
        /// Executes an asynchronous operation with a timeout.
        /// </summary>
        /// <param name="operation">The asynchronous operation to execute.</param>
        /// <param name="message">The message to display to the console before executing the operation.</param>
        /// <returns>A task representing the completion of the operation.</returns>
        /// <exception cref="TimeoutException">Thrown if the operation does not complete within the specified timeout.</exception>
        public static async Task ExecuteOperationAsync(Func<Task> operation, string message)
        {
            Console.Write($"{Properties.ColorSuccess}{message}{Properties.ColorReset}");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            try
            {
                var task = operation();
                var timeoutTask = Task.Delay(Timeout.Infinite, cts.Token);

                await Task.WhenAny(task, timeoutTask);
                if (!task.IsCompleted)
                {
                    throw new TimeoutException("The operation did not complete within the allotted time.");
                }
                await task; // Re-throws any exception that occurred during the operation
            }
            finally
            {
                cts.Cancel();
            }
        }

        /// <summary>
        /// Shows a spinner animation in the console.
        /// </summary>
        /// <param name="ct">A cancellation token to stop the spinner.</param>
        /// <param name="maxIterations">The maximum number of iterations for the spinner (optional).</param>
        /// <returns>A task representing the completion of the spinner animation.</returns>
        public static async Task ShowSpinnerAsync(CancellationToken ct, int maxIterations = int.MaxValue)
        {
            var spinner = new[] { '⣾', '⣽', '⣻', '⢿', '⡿', '⣟', '⣯', '⣷' };
            for (int i = 0; i < maxIterations && !ct.IsCancellationRequested; i++)
            {
                Console.Write(spinner[i % spinner.Length]);
                await Task.Delay(100);
                Console.Write("\b \b");
            }
        }

        /// <summary>
        /// Handles and displays an error to the console.
        /// Differentiates between running in test mode and production mode.
        /// </summary>
        /// <param name="ex">The exception to handle.</param>
        public static void HandleError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;

            if (!IsRunningInTestMode())
            {
                Console.WriteLine($"\u001b[31mERROR: {ex.Message}\u001b[0m");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey(); // Only in production code
            }
            else
            {
                Console.WriteLine($"\u001b[31mTEST ERROR: {ex.Message}\u001b[0m");
            }

            Console.ResetColor();
        }

        /// <summary>
        /// Prints an error message to the console in red.
        /// </summary>
        /// <param name="message">The error message to print.</param>
        public static void PrintError(string message) =>
            Console.WriteLine($"{Properties.ColorError}{message}{Properties.ColorReset}");

        /// <summary>
        /// Prints a success message to the console in blue.
        /// </summary>
        /// <param name="message">The success message to print.</param>
        public static void PrintSuccess(string message) =>
            Console.WriteLine($"{Properties.ColorSuccess}{message}{Properties.ColorReset}");

        /// <summary>
        /// Cleans a string to be used as a name by removing any characters that are not alphanumeric, hyphens, or underscores.
        /// The resulting string is also converted to lowercase.
        /// </summary>
        /// <param name="name">The name to clean.</param>
        /// <returns>The cleaned name in lowercase, or an empty string if the input is null or whitespace.</returns>
        public static string CleanName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Allow underscore and other safe characters
            var cleaned = Regex.Replace(name.Trim(), @"[^\w-]", "");
            return cleaned.ToLowerInvariant();
        }

        /// <summary>
        /// Prints a header to the console.
        /// </summary>
        public static void ResetConsole()
        {
            Console.WriteLine($"\n{Properties.ColorSuccess}=== AZURE KEYVAULT PASSWORD MANAGEMENT ==={Properties.ColorReset}\n");
        }

        /// <summary>
        /// Decodes a URI-encoded string.
        /// </summary>
        /// <param name="encodedName">The URI-encoded string to decode.</param>
        /// <returns>The decoded string.</returns>
        public static string DecodeName(string encodedName)
        {
            return Uri.UnescapeDataString(encodedName);
        }

        /// <summary>
        /// Encodes a string for use in a URI.
        /// </summary>
        /// <param name="name">The string to encode.</param>
        /// <returns>The URI-encoded string.</returns>
        public static string EncodeName(string name)
        {
            return Uri.EscapeDataString(name);
        }

        /// <summary>
        /// Reads a line of input from the console.
        /// </summary>
        /// <param name="prompt">The prompt to display to the user.</param>
        /// <returns>The input read from the console, or null if no input was provided.</returns>
        public static string ReadInput(string prompt)
        {
            Console.Write($"{prompt}: ");
            return Console.ReadLine()?.Trim();
        }

        /// <summary>
        /// Reads password input from the console securely using SecurityHelper.
        /// </summary>
        /// <param name="prompt">The prompt to display to the user (default is "Password").</param>
        /// <returns>The password entered by the user as a string.</returns>
        public static string ReadPassword(string prompt = "Password")
        {
            Console.Write($"{prompt}: ");
            return SecurityHelper.ReadSecurePassword();
        }

        /// <summary>
        /// Checks if the application is currently running in a test environment.
        /// </summary>
        /// <returns>True if running in a test environment, otherwise false.</returns>
        private static bool IsRunningInTestMode()
        {
            return AppDomain.CurrentDomain.FriendlyName.Contains("testhost")
                   || Environment.GetEnvironmentVariable("UNITTEST") == "1";
        }
    }
}