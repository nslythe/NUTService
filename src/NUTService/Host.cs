using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace NUTService
{
    public class Host
    {
        [Flags]
        enum ShutdownFlags : UInt32
        {
            FORCE_OTHERS = 0x00000001,
            FORCE_SELF = 0x00000002,
            RESTART = 0x00000004,
            POWEROFF = 0x00000008,
            NOREBOOT = 0x00000010,
            GRACE_OVERRIDE = 0x00000020,
            INSTALL_UPDATES = 0x00000040,
            RESTARTAPPS = 0x00000080,
            HYBRID = 0x00000200 
        };

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern UInt32 InitiateShutdown(
            string lpMachineName,
            string lpMessage,
            UInt32 dwGracePeriod,
            UInt32 dwShutdownFlags,
            UInt32 dwReason);

        public static void Shutdown(string msg, uint grace)
        {
            ShutdownFlags f = ShutdownFlags.FORCE_SELF & ShutdownFlags.FORCE_OTHERS & ShutdownFlags.POWEROFF;
            uint return_code = InitiateShutdown(Environment.MachineName, msg, grace, f, 0x00060000);

            // 1190 = ERROR_SHUTDOWN_IS_SCHEDULED
            if (return_code != 0 && return_code != 1190)
            {
                throw new Exception($"{return_code}");
            }
        }
    }
}
