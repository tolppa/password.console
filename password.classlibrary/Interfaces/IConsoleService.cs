namespace password.classlibrary.Interfaces
{
    /// <summary>
    /// Defines an interface for a console service, providing a method to run the service asynchronously.
    /// </summary>
    public interface IConsoleService
    {
        /// <summary>
        /// Executes the main operation of the console service asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task RunAsync();
    }
}