using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace NUTService
{
    public class NUTVar
    {
        public string name { get; private set; }
        public string ups_name { get; private set; }
        public string value { get; private set; }

        public NUTVar(string un, string n, string v)
        {
            ups_name = un;
            name = n;
            value = v;
        }

        public static NUTVar FromString(string value)
        {
            Regex re = new Regex("(VAR )?(?<ups_name>\\S+) (?<var_name>\\S+) \"(?<value>[^\"]+)\"");

            Match match = re.Match(value);

            return new NUTVar(match.Groups["ups_name"].Value, match.Groups["var_name"].Value, match.Groups["value"].Value);
        }
    }
}
