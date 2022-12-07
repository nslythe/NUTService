using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUTService
{
    internal class NutException : Exception
    {
        private string m_message;
        public NutException(string msg)
        {
            m_message = msg;
        }
    }
}
