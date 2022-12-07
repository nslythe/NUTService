using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace NUTService
{
    public class Host
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        static extern UInt32 InitiateShutdown(
            string lpMachineName,
            string lpMessage,
            UInt32 dwGracePeriod,
            UInt32 dwShutdownFlags,
            UInt32 dwReason);

        public static void Shutdown(string msg, uint grace)
        {
            uint return_code = InitiateShutdown(Environment.MachineName, msg, grace, 1, 0x00060000);
            if (return_code != 0)
            {
                throw new Exception($"{return_code}");
            }
        }
    }
}
