using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NUTService
{
    class Logger
    {
        private const string logName = "NUTClientService";
        private EventLog m_log;

        public Logger(string source)
        {
            if (!EventLog.SourceExists(source))
            {
                EventLog.CreateEventSource(source, logName);
            }

            m_log = new EventLog(logName);
            m_log.Source = source;
        }

        ~Logger()
        {
            m_log.Close();
        }

        public void Info(string msg)
        {
            m_log.WriteEntry(msg, EventLogEntryType.Information);
        }
        public void Warn(string msg)
        {
            m_log.WriteEntry(msg, EventLogEntryType.Warning);
        }
        public void Error(string msg)
        {
            m_log.WriteEntry(msg, EventLogEntryType.Error);
        }
    }

    public partial class NUTService : ServiceBase
    {
        private Logger m_logger;
        private NUTClient m_client;
        private bool m_do_stop;
        private Thread m_thread;
        private Config m_config;

        public NUTService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            m_do_stop = false;

            m_logger = new Logger("NUTClient - Service");

            m_logger.Info($"Loading config from : {Config.GetConfigPath()}");
            try
            {
                m_config = Config.Load();
                m_logger.Info($"Config loaded");
            }
            catch (Exception e)
            {
                m_logger.Error($"Failed to load config : {e.Message}");
                Environment.Exit(1);
            }

            m_client = new NUTClient(m_config.host);

            m_thread = new Thread(Run);
            m_thread.Start();
        }

        private void Connect()
        {
            while (!m_do_stop)
            {
                try
                {
                    m_client.Connect();
                    break;
                }
                catch (Exception e)
                {
                    m_logger.Error($"Unable to connect will retry : {e.Message}");
                }
                Thread.Sleep(30000);
            }
        }

        private void Run()
        {
            m_logger.Info($"Service started");
            while (!m_do_stop)
            {
                Connect();
                m_logger.Info($"Connected to NUT server {m_config.host}");

                try
                {
                    m_client.Username(m_config.username);
                    m_client.Password(m_config.password);
                    m_logger.Info($"Username / password accepted by server");
                }
                catch (Exception e)
                {
                    m_logger.Error($"Failed to set username/password {e.Message}");
                    break;
                }


                NUTUPS ups = new NUTUPS(m_config.ups);

                try
                {
                    m_client.Login(ups);
                    m_logger.Info($"Login to UPS : {ups.name}");
                }
                catch (Exception e)
                {
                    m_logger.Error(e.Message);
                }

                MainLoop(ups);

                Thread.Sleep(3000);
            }

            m_client.Logout();
            m_logger.Info($"Service stoped");
        }

        private void MainLoop(NUTUPS ups)
        {
            NUTClient.NUTState last_state = 0;
            NUTClient.NUTState current_state = 0;

            while (!m_do_stop)
            {
                try
                {
                    current_state = m_client.GetUPSState(ups);
                    if (last_state != current_state)
                    {
                        m_logger.Info($"{ups.name} state changed {NUTClient.NUTStateToString(last_state)} -> {NUTClient.NUTStateToString(current_state)}");
                    }

                    if (m_config.shutdown_on_lowe_battery && 
                        (current_state & NUTClient.NUTState.LB) == NUTClient.NUTState.LB)
                    {
                        m_logger.Warn($"UPS state LB (shutdown)");
                        try
                        {
                            Host.Shutdown($"Receive low battery from NUT: {ups.name}@{m_config.host}", m_config.grace_delay);
                        }
                        catch (Exception e)
                        {
                            m_logger.Error($"Unable to shutdown the system : {e.Message}");
                        }
                    }

                    if ((current_state & NUTClient.NUTState.FSD) == NUTClient.NUTState.FSD)
                    {
                        m_logger.Warn($"UPS state FSD (Force to shutdown)");
                        try
                        {
                            Host.Shutdown($"Receive force shutdown from NUT: {ups.name}@{m_config.host}", m_config.grace_delay);
                        }catch(Exception e)
                        {
                            m_logger.Error($"Unable to shutdown the system : {e.Message}");
                        }
                    }

                    last_state = current_state;
                }
                catch (Exception e)
                {
                    m_logger.Error(e.Message);
                    break;
                }
                Thread.Sleep(3000);
            }
        }


        protected override void OnStop()
        {
            m_logger.Info($"Service stoping");
            m_do_stop = true;
            m_thread.Join();
        }
    }
}
