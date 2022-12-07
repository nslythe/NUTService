using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;


namespace NUTService
{
    public class Config
    {
        public string host { get; set; }
        public string ups { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public uint grace_delay { get; set; } = 30;
        public bool shutdown_on_lowe_battery { get; set; } = false;

        public static string GetConfigPath()
        {
            string file_name = "config.json";
            string exe_dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(exe_dir, file_name);
        }

        public static Config Load()
        {
            string jsonString = File.ReadAllText(GetConfigPath());
            Config config = JsonConvert.DeserializeObject<Config>(jsonString);
            return config;
        }

        public static void GenDefaultConfig()
        {
            Config c = new Config();
            string jsonString = JsonConvert.SerializeObject(c, Formatting.Indented);
            File.WriteAllText(GetConfigPath(), jsonString);
        }
    }
}
