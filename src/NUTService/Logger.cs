using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUTService
{
    class Logger
    {
        private const string logName = "NUTService";
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
}
