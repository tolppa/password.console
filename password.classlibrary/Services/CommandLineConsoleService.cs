using password.classlibrary.Utils;

namespace password.classlibrary.Services
{
    ///
    /// A console service that can parse command-line arguments
    /// or fall back to the interactive menu. Shared interactive logic
    /// is inherited from .
    public class CommandLineConsoleService : BaseConsoleService
    {
        ///
        /// Initializes a new instance of the class.
        ///
        /// A configured .
        /// The logging service to use.
        /// The initial Key Vault URL.
        /// The path to the configuration file.
        public CommandLineConsoleService(KeyVaultService keyVaultService,LoggingService loggingService,
            string keyVaultUrl, string configPath)
        : base(keyVaultService, loggingService, keyVaultUrl, configPath)
        {
        }

        /// <summary>  
        /// Runs command-line parsing if arguments are provided,  
        /// otherwise launches the interactive console loop.  
        /// </summary>
        public override async Task RunAsync()
        {
            string[] args = Environment.GetCommandLineArgs();

            // The first argument is usually the application path, so look at index 1  
            if (args.Length > 1)
            {
                await HandleCommandLineArguments([.. args.Skip(1)]);
            }
            else
            {
                await RunInteractiveConsole();
            }
        }

        /// <summary>  
        /// Handles command-line arguments such as "-get" and "-list".  
        /// Falls back to showing help if invalid.  
        /// </summary>  
        /// <param name="args">Array of arguments (excluding the .exe path).</param> 
        protected async Task HandleCommandLineArguments(string[] args)
        {
            try
            {
                switch (args[0].ToLower())
                {
                    case "-get":
                        if (args.Length == 2)
                        {
                            // Reuse BaseConsoleService logic to fetch a password  
                            var secretName = args[1];
                            await Helpers.ExecuteOperationAsync(async () =>
                            {
                                var secretValue = await _keyVaultService.GetSecretAsync(secretName);
                                Console.WriteLine(secretValue);
                            }, "Get");
                        }
                        else
                        {
                            Helpers.PrintError("Usage: -get \"SECRET_NAME\"");
                        }
                        break;

                    case "-list":
                        string searchTerm = args.Length > 1 ? args[1] : null;
                        await Helpers.ExecuteOperationAsync(async () =>
                        {
                            var secrets = await _keyVaultService.ListSecretsAsync(searchTerm);
                            if (secrets.Count == 0) throw new Exception("No results found");
                            secrets.ForEach(s => Console.WriteLine(s));
                        }, "List");
                        break;

                    default:
                        Helpers.PrintError($"Unknown command: {args[0]}");
                        ShowCommandLineHelp();
                        break;
                }
            }
            catch (Exception ex)
            {
                Helpers.PrintError($"Error handling command line arguments: {ex.Message}");
                _loggingService.LogError(ex.Message);
            }
        }

        /// <summary>  
        /// Displays help text for users of command-line arguments.  
        /// </summary>  
        private static void ShowCommandLineHelp()
        {
            Console.WriteLine("\nValid commands:");
            Console.WriteLine("  -get \"SECRET_NAME\"   : Show the value of a secret");
            Console.WriteLine("  -list [SEARCH_TERM]   : List secrets (optionally filter by term)");
            Console.WriteLine("No arguments runs the interactive console menu.");
        }

    }
}