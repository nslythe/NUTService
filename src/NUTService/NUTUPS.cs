using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace NUTService
{
    public class NUTUPS
    {
        public string name { get; private set; }
        public string description { get; private set; }

        public NUTUPS(string n)
        {
            name = n;
        }

        public NUTUPS(string n, string d)
        {
            name = n;
            description = d;
        }

        public static NUTUPS FromString(string value)
        {
            Regex re = new Regex("(UPS )?(?<name>\\S+) \"(?<description>[^\"]+)\"");

            Match match = re.Match(value);

            return new NUTUPS(match.Groups["name"].Value, match.Groups["description"].Value);
        }
    }
}
