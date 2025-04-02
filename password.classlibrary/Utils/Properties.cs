namespace password.classlibrary.Utils
{
    /// <summary>
    /// Provides static properties for console output formatting, such as ANSI color codes.
    /// </summary>
    public static class Properties
    {
        /// <summary>
        /// ANSI escape code for red color, used to indicate errors.
        /// This will be an empty string if the 'NO_COLOR' environment variable is set.
        /// </summary>
        public static readonly string ColorError =
            Environment.GetEnvironmentVariable("NO_COLOR") == null ? "\x1b[31m" : "";

        /// <summary>
        /// ANSI escape code for blue color, used to indicate success or important information.
        /// This will be an empty string if the 'NO_COLOR' environment variable is set.
        /// </summary>
        public static readonly string ColorSuccess =
            Environment.GetEnvironmentVariable("NO_COLOR") == null ? "\x1b[34m" : "";

        /// <summary>
        /// ANSI escape code to reset the console color to the default.
        /// This will be an empty string if the 'NO_COLOR' environment variable is set.
        /// </summary>
        public static readonly string ColorReset =
            Environment.GetEnvironmentVariable("NO_COLOR") == null ? "\x1b[0m" : "";
    }
}