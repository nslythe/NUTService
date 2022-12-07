using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Timers;

namespace NUTService
{
    public partial class NUTService : ServiceBase
    {
        private Logger m_logger;
        private NUTClient m_client = null;
        private Timer m_timer;
        private Config m_config;
        private NUTUPS m_ups;
        private NUTClient.NUTState m_last_state = 0;
        private NUTClient.NUTState m_current_state = 0;


        public NUTService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            m_logger = new Logger("NUTService");

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

            m_ups = new NUTUPS(m_config.ups);

            m_timer = new Timer();
            m_timer.Interval = 10000; // 60 seconds
            m_timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            m_timer.Start();
            m_logger.Info($"Service started");
        }

        private void Connect()
        {
            m_last_state = 0;
            m_current_state = 0;

            try
            {
                m_client = null;
                m_client = new NUTClient(m_config.host);
                m_client.Connect();
            }
            catch (Exception e)
            {
                m_logger.Error($"Unable to connect will retry : {e.Message}");
                throw e;
            }
        }

        private void SetUserPassword()
        {
            try
            {
                m_client.Username(m_config.username);
                m_client.Password(m_config.password);
                m_logger.Info($"Username / password accepted by server");
            }
            catch (Exception e)
            {
                m_logger.Error($"Failed to set username/password {e.Message}");
                throw e;
            }
        }

        private void Login()
        {
            try
            {
                m_client.Login(m_ups);
                m_logger.Info($"Login to UPS : {m_ups.name}");
            }
            catch (Exception e)
            {
                m_logger.Error(e.Message);
                throw e;
            }
        }

        private void PollAndProcessStates()
        {
            try
            {
                m_current_state = m_client.GetUPSState(m_ups);
                if (m_last_state != m_current_state)
                {
                    m_logger.Info($"{m_ups.name} state changed {NUTClient.NUTStateToString(m_last_state)} -> {NUTClient.NUTStateToString(m_current_state)}");
                }

                if (m_config.shutdown_on_lowe_battery &&
                    (m_current_state & NUTClient.NUTState.LB) == NUTClient.NUTState.LB)
                {
                    m_logger.Warn($"UPS state LB (shutdown)");
                    try
                    {
                        Host.Shutdown($"Receive low battery from NUT: {m_ups.name}@{m_config.host}", m_config.grace_delay);
                        m_logger.Info($"Shutdown command executed");
                    }
                    catch (Exception e)
                    {
                        m_logger.Error($"Unable to shutdown the system : {e.Message}");
                    }
                }

                if ((m_current_state & NUTClient.NUTState.FSD) == NUTClient.NUTState.FSD)
                {
                    m_logger.Warn($"UPS state FSD (Force to shutdown)");
                    try
                    {
                        Host.Shutdown($"Receive force shutdown from NUT: {m_ups.name}@{m_config.host}", m_config.grace_delay);
                        m_logger.Info($"Shutdown command executed");
                    }
                    catch (Exception e)
                    {
                        m_logger.Error($"Unable to shutdown the system : {e.Message}");
                    }
                }

                m_last_state = m_current_state;
            }
            catch (Exception e)
            {
                m_logger.Error(e.Message);
                throw e;
            }
        }

        private void OnTimer(object sender, ElapsedEventArgs args)
        {
            m_timer.Enabled = false;
            try
            {
                if (m_client == null || !m_client.IsConnected())
                {
                    Connect();
                    m_logger.Info($"Connected to NUT server {m_config.host}");
                    SetUserPassword();
                    Login();
                }

                PollAndProcessStates();
            }
            finally
            {
                m_timer.Enabled = true;
            }
        }


        protected override void OnStop()
        {
            m_logger.Info($"Service stoping");
            m_timer.Stop();
            
            //TODO: wait call finished
            m_logger.Info($"Service stoped");
        }
    }
}
