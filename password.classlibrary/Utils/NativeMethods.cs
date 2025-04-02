using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("password.console.tests")]
namespace password.classlibrary.Utils
{
    /// <summary>
    /// Provides access to native Windows API methods.
    /// </summary>
    internal static class NativeMethods
    {
        /// <summary>
        /// Sends the specified message to a set of window handles.
        /// </summary>
        /// <param name="hWnd">A handle to the window whose window procedure will receive the message. If this parameter is HWND_BROADCAST, the message is sent to all top-level windows in the system.</param>
        /// <param name="Msg">The message to be sent.</param>
        /// <param name="wParam">Additional message-specific information.</param>
        /// <param name="lParam">Additional message-specific information.</param>
        /// <param name="fuFlags">Specifies the nature of the call. This parameter can be one or more of the following values. SMTO_ABORTIFHUNG: Returns without waiting for the time-out period to elapse if the receiving process is hung.</param>
        /// <param name="uTimeout">Specifies the duration, in milliseconds, of the time-out period. If the message is a broadcast message, each window can use the full time-out period.</param>
        /// <param name="lpdwResult">A pointer to a variable that receives the result of the message processing. This value depends on the message.</param>
        /// <returns>The return value specifies the result of the message processing; it depends on the message that is sent.</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            UIntPtr wParam,
            string lParam,
            uint fuFlags,
            uint uTimeout,
            out UIntPtr lpdwResult);

        /// <summary>
        /// Represents a handle to all top-level windows in the system.
        /// </summary>
        public const nint HWND_BROADCAST = 0xffff;

        /// <summary>
        /// The WM_SETTINGCHANGE message is sent to all top-level windows after the system makes a change to the system-wide settings.
        /// </summary>
        public const uint WM_SETTINGCHANGE = 0x001A;

        /// <summary>
        /// Returns without waiting for the time-out period to elapse if the receiving process is hung.
        /// Used with the SendMessageTimeout function.
        /// </summary>
        public const uint SMTO_ABORTIFHUNG = 0x0002;
    }
}