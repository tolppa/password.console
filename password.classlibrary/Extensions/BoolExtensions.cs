namespace password.classlibrary.Extensions
{

    /// <summary>
    /// Provides an extension method to convert a boolean value to a user-friendly "Yes" or "No" string.
    /// </summary>
    internal static class BoolExtensions
    {
        /// <summary>
        /// Converts a boolean value to a string representation of "Yes" or "No".
        /// </summary>
        /// <param name="value">The boolean value to convert.</param>
        /// <returns>"Yes" if the input value is true; otherwise, "No".</returns>
        internal static string ToYesNo(this bool value) => value ? "Yes" : "No";
    }

}