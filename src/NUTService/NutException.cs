using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUTService
{
    internal class NutException : Exception
    {
        public NutException(string msg) : base(msg)
        {
        }
    }
}
