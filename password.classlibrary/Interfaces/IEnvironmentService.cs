namespace password.classlibrary.Interfaces
{
    /// <summary>
    /// Defines an interface for managing the application's path in the system environment variables.
    /// </summary>
    public interface IEnvironmentService
    {
        /// <summary>
        /// Registers the application's directory path to the system's PATH environment variable.
        /// </summary>
        /// <param name="interactive">A boolean indicating if the registration is happening in an interactive mode, potentially prompting the user if needed.</param>
        void RegisterApplicationPath(bool interactive = true);

        /// <summary>
        /// Unregisters the application's directory path from the system's PATH environment variable.
        /// </summary>
        void UnregisterApplicationPath();

        /// <summary>
        /// Checks if the application's directory path is currently registered in the system's PATH environment variable.
        /// </summary>
        /// <returns><c>true</c> if the path is registered; otherwise, <c>false</c>.</returns>
        bool IsRegistered();
    }
}