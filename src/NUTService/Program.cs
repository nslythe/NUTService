using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace NUTService
{
    internal static class Program
    {
        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        static void Main(string[] args)
        {
            // getn config
            // Config.GenDefaultConfig();

            Config m_config = Config.Load();

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new NUTService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
