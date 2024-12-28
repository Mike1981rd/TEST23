using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Printer
{
    public class UserSession
    {
        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSQueryUserToken(uint sessionId, out IntPtr token);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool DuplicateTokenEx(
            IntPtr existingToken,
            uint dwDesiredAccess,
            IntPtr lpTokenAttributes,
            int impersonationLevel,
            int tokenType,
            out IntPtr duplicateToken);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public uint cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        public static bool LaunchProcessInUserSession(string exePath)
        {
            IntPtr userToken = IntPtr.Zero;
            IntPtr duplicatedToken = IntPtr.Zero;

            try
            {
                uint sessionId = WTSGetActiveConsoleSessionId();

                if (!WTSQueryUserToken(sessionId, out userToken))
                {
                    Console.WriteLine("Error: No se pudo obtener el token de usuario activo.");
                    return false;
                }

                if (!DuplicateTokenEx(userToken, 0xF01FF, IntPtr.Zero, 2, 1, out duplicatedToken))
                {
                    Console.WriteLine("Error: No se pudo duplicar el token de usuario.");
                    return false;
                }

                STARTUPINFO startupInfo = new STARTUPINFO();
                PROCESS_INFORMATION processInfo = new PROCESS_INFORMATION();

                string command = $"\"{exePath}\"";

                if (!CreateProcessAsUser(
                    duplicatedToken,
                    null,
                    command,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    0,
                    IntPtr.Zero,
                    null,
                    ref startupInfo,
                    out processInfo))
                {
                    Console.WriteLine("Error: No se pudo iniciar el proceso en la sesión del usuario.");
                    return false;
                }

                CloseHandle(processInfo.hProcess);
                CloseHandle(processInfo.hThread);

                return true;
            }
            finally
            {
                if (userToken != IntPtr.Zero) CloseHandle(userToken);
                if (duplicatedToken != IntPtr.Zero) CloseHandle(duplicatedToken);
            }
        }

        [DllImport("kernel32.dll")]
        private static extern uint WTSGetActiveConsoleSessionId();
    }
}
