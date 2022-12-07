using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Timers;
using System.Threading;

namespace NUTService
{
    class ShutdownEventArgs : EventArgs
    {
        public string Message { get; private set;}
        public uint Grace { get; private set; }

        public ShutdownEventArgs(string msg, uint grace)
        {
            Message = msg;
            Grace = grace;
        }
    }

    public partial class NUTService : ServiceBase
    {
        private Logger m_logger;
        private NUTClient m_client = null;
        private System.Timers.Timer m_timer;
        private Config m_config;
        private NUTUPS m_ups;
        private NUTClient.NUTState m_last_state = NUTClient.NUTState.UNKNOWN;
        private NUTClient.NUTState m_current_state = NUTClient.NUTState.UNKNOWN;
        private NUTClient.NUTAlarmState m_alarm_state = 0;
        private event EventHandler<ShutdownEventArgs> ShutdownEvent;

        public NUTService()
        {
            InitializeComponent();

            m_logger = new Logger("NUTService");
            ShutdownEvent += new EventHandler<ShutdownEventArgs>(this.OnShutdownSystem);
        }

        protected override void OnStart(string[] args)
        {
#if DEBUG
            //Thread.Sleep(new TimeSpan(0, 0, 20));
#endif

            m_timer = new System.Timers.Timer();
            m_timer.Interval = 10000;
            m_timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            m_timer.Start();
            m_logger.Info($"Service started");
        }

        protected override void OnShutdown()
        {
            m_logger.Info($"Service is shutting down");
            try
            {
                m_client.Logout();
            }catch(Exception)
            { }
        }

        private void LoadConfig()
        {
            m_logger.Info($"Loading config from : {Config.GetConfigPath()}");
            try
            {
                m_config = Config.Load();
                m_logger.Info($"Config loaded");
            }
            catch (Exception e)
            {
                m_logger.Error($"Failed to load config : {e.Message}");
                m_config = null;
                m_ups = null;
            }

        }
        private void Connect()
        {
            try
            {
                m_client = null;
                m_client = new NUTClient(m_config.Data.host);
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
                m_client.Username(m_config.Data.username);
                m_client.Password(m_config.Data.password);
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
                m_alarm_state = NUTClient.NUTStateToAlarmState(m_current_state);

                if (m_last_state != m_current_state)
                {
                    m_logger.Info($"{m_ups.name} state changed {NUTClient.NUTStateToString(m_last_state)} -> {NUTClient.NUTStateToString(m_current_state)}");
                }

                if ((m_current_state & NUTClient.NUTState.OB) != 0)
                {
                    m_logger.Warn($"{m_ups.name} is on battery");
                }

                if ((m_current_state & NUTClient.NUTState.DISCHRG) != 0)
                {
                    m_logger.Warn($"{m_ups.name} is discharging");
                }

                if ((m_current_state & NUTClient.NUTState.LB) != 0)
                {
                    m_logger.Error($"{m_ups.name} is low battery");
                    if (m_config.Data.shutdown_on_low_battery)
                    {
                        ShutdownEvent.Invoke(this, new ShutdownEventArgs($"Receive low battery from NUT: {m_ups.name}@{m_config.Data.host}", m_config.Data.grace_delay));
                    }
                }

                if ((m_current_state & NUTClient.NUTState.FSD) != 0)
                {
                    ShutdownEvent.Invoke(this, new ShutdownEventArgs($"Receive force shutdown from NUT: {m_ups.name}@{m_config.Data.host}", m_config.Data.grace_delay));
                }

                m_last_state = m_current_state;
            }
            catch (Exception e)
            {
                m_logger.Error(e.Message);
                throw e;
            }
        }

        internal void Loop()
        {
            if (m_config != null && m_config.NeedReload)
            {
                m_logger.Info($"Config modified will reload");
            }

            bool config_changed = false;
            if (m_config == null || m_config.NeedReload)
            {
                LoadConfig();

                m_ups = new NUTUPS(m_config.Data.ups);

                m_last_state = NUTClient.NUTState.UNKNOWN;
                m_current_state = NUTClient.NUTState.UNKNOWN;
                config_changed = true;
            }

            if (m_config != null)
            {
                if (m_client == null || !m_client.IsConnected() || config_changed)
                {
                    Connect();
                    m_logger.Info($"Connected to NUT server {m_config.Data.host}");
                    SetUserPassword();
                    Login();
                }

                PollAndProcessStates();
            }
        }

        private void OnTimer(object sender, ElapsedEventArgs args)
        {
            m_timer.Enabled = false;

            try
            {
                Loop();
            }
            finally
            {
                m_timer.Enabled = true;
            }
        }

        private void OnShutdownSystem(object sender, ShutdownEventArgs args)
        {
            try
            {
                m_logger.Warn(args.Message);
                Host.Shutdown(args.Message, args.Grace);
                m_logger.Info($"Shutdown command executed");
            }
            catch (Exception e)
            {
                m_logger.Error($"Unable to shutdown the system : {e.Message}");
            }
        }

        protected override void OnStop()
        {
            m_logger.Info($"Service stoping");
            m_timer.Stop();

            //TODO: wait call finished
            try
            {
                m_client.Logout();
            }catch(Exception)
            { }
            m_logger.Info($"Service stoped");
        }
    }
}
