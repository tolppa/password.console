namespace password.classlibrary.Interfaces
{
    /// <summary>
    /// Defines an interface for a logging service to record application operations and errors.
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// Logs an application operation with its type and the name of the entity involved.
        /// </summary>
        /// <param name="operation">The type of operation performed (e.g., "Add", "Delete", "Update").</param>
        /// <param name="name">The name of the entity that the operation was performed on (e.g., secret name).</param>
        void LogOperation(string operation, string name);

        /// <summary>
        /// Logs an error that occurred during the application execution.
        /// </summary>
        /// <param name="error">The error message to be logged.</param>
        void LogError(string error);
    }
}