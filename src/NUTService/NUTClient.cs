using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;


//TODO support TLS - STARTTLS

namespace NUTService
{
    public class NUTClient
    {

        [Flags]
        public enum NUTState
        {
            UNKNOWN = 1,
            OL = 2,         // Online           - OK
            OB = 4,         // On battery       - WARNING
            DISCHRG = 8,    // Discharging      - WARNING
            CHRG = 16,       // Charging         - OK
            LB = 32,        // Low battery      - CRITICAL
            FSD = 64        // Force shutdown   - CRITICAL
        }

        public enum NUTAlarmState
        {
            OK,
            WARNING,
            CRITICAL
        }


        public static  string NUTStateToString(NUTState state)
        {
            List<string> state_list = new List<string>();
            foreach(NUTState s in Enum.GetValues(typeof(NUTState)))
            {
                if ((state & s) == s)
                {
                    state_list.Add(Enum.GetName(typeof(NUTState), s));
                }
            }
            return String.Join(", ", state_list);
        }

        public static NUTAlarmState NUTStateToAlarmState(NUTState state)
        {
            if (
                (state & NUTState.LB) != 0 ||
                (state & NUTState.FSD) != 0
            )
            {
                return NUTAlarmState.CRITICAL;
            }

            if (
                (state & NUTState.DISCHRG) != 0 ||
                (state & NUTState.OB) != 0
            )
            {
                return NUTAlarmState.WARNING;
            }
            
            return NUTAlarmState.OK;
        }


        const int NUT_SERVER_PORT = 3493;

        private string m_server_address;
        private TcpClient m_tcp_client;
        private StreamWriter m_socket_writer;
        private StreamReader m_socket_reader;

        public NUTClient(String server_address)
        {
            m_server_address = server_address;
            m_tcp_client = new TcpClient();
            m_tcp_client.ReceiveTimeout = 5000;
        }

        ~NUTClient()
        {
            if (m_tcp_client.Connected)
            {
                m_tcp_client.Close();
            }
        }

        public void Connect()
        {
            IPAddress server_ipaddress;
            if (!IPAddress.TryParse(m_server_address, out server_ipaddress))
            {
                IPHostEntry server_host_entry = Dns.GetHostEntry(m_server_address);
                server_ipaddress = server_host_entry.AddressList[0];
            }
            m_tcp_client.Connect(server_ipaddress, NUT_SERVER_PORT);

            m_socket_writer = new StreamWriter(m_tcp_client.GetStream(), Encoding.ASCII);
            m_socket_reader = new StreamReader(m_tcp_client.GetStream(), Encoding.ASCII);

            string serve_description = ExecuteCommand("VER");
            string protocol_version = ExecuteCommand("NETVER");
        }

        public bool IsConnected()
        {
            return m_tcp_client.Connected;
        }

        public void Logout()
        {
            ExecuteCommand("LOGOUT");
        }

        public void Login(NUTUPS ups)
        {
            ExecuteCommand($"LOGIN {ups.name}");
        }

        public void Password(string password)
        {
            ExecuteCommand($"PASSWORD {password}");
        }

        public void Username(string username)
        {
            ExecuteCommand($"USERNAME {username}");
        }

        public IEnumerable<NUTUPS> ListUPS()
        {
            List<NUTUPS> return_value = new List<NUTUPS>();
            foreach (string v in ExecuteCommandList("LIST UPS"))
            {
                return_value.Add(NUTUPS.FromString(v));
            }
            return return_value;
        }

        public IEnumerable<NUTVar> ListVAR(NUTUPS ups)
        {
            List<NUTVar> return_value = new List<NUTVar>();
            foreach (string v in ExecuteCommandList($"LIST VAR {ups.name}"))
            {
                return_value.Add(NUTVar.FromString(v));
            }
            return return_value;
        }

        public NUTState GetUPSState(NUTUPS ups)
        {
            string value = ExecuteCommand($"GET VAR {ups.name} ups.status");

            NUTVar var = NUTVar.FromString(value);

            NUTState state = 0;

            foreach(string v in var.value.Split(' '))
            {
                NUTState vstate;
                if (!Enum.TryParse(v, out vstate))
                {
                    throw new Exception("enum does not match");
                }
                state |= vstate;
            }

            return state;
        }



        private string ExecuteCommand(string cmd)
        {
            m_socket_writer.WriteLine(cmd);
            m_socket_writer.Flush();
            
            string response = m_socket_reader.ReadLine();

            if (response == null)
            {
                m_tcp_client.Close();
                throw new NutException("No response from NUT server");
            }
            if (response.StartsWith("ERR"))
            {
                throw new NutException(response);
            }
            return response;
        }

        private string[] ExecuteCommandList(string cmd)
        {
            string response = ExecuteCommand(cmd);

            if (!response.StartsWith("BEGIN LIST"))
            {
                throw new Exception("Bad type");
            }

            List<string> return_value = new List<string>();
            while (true)
            {
                response = m_socket_reader.ReadLine();
                if (response == null)
                {
                    m_tcp_client.Close();
                    throw new NutException("No response from NUT server");
                }
                if (!response.StartsWith("END LIST"))
                {
                    return_value.Add(response);
                }
                else
                {
                    break;
                }
            }
            return return_value.ToArray();
        }
    }
}
