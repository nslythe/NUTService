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
using System.ComponentModel.Design;

namespace NUTService
{
    public class Config
    {
        public class ConfigData
        {
            public string host { get; set; }
            public string ups { get; set; }
            public string username { get; set; } = "";
            public string password { get; set; } = "";
            public uint grace_delay { get; set; } = 30;
            public bool shutdown_on_low_battery { get; set; } = false;
        }

        public bool NeedReload { get; private set; } = false;
        private FileSystemWatcher m_watcher;
        public ConfigData Data { get; private set; }

        private Config(ConfigData data, string file_path = null)
        {
            Data = data;
            if (!string.IsNullOrEmpty(file_path))
            {
                m_watcher = new FileSystemWatcher();
                m_watcher.Path = GetConfigDir();
                m_watcher.NotifyFilter = NotifyFilters.CreationTime |
                    NotifyFilters.LastWrite |
                    NotifyFilters.Size;
                m_watcher.Filter = "config.json";
                m_watcher.Changed += new FileSystemEventHandler(OnConfigChanged);
                m_watcher.Created += new FileSystemEventHandler(OnConfigChanged);
                m_watcher.EnableRaisingEvents = true;
            }
        }

        public void OnConfigChanged(object source, FileSystemEventArgs e)
        {
            NeedReload = true;
        }

        public static string GetConfigDir()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public static string GetConfigPath(string file_name = "config.json")
        {
            return Path.Combine(GetConfigDir(), file_name);
        }

        public static Config Load()
        {
            if (!File.Exists(GetConfigPath()))
            {
                if (!File.Exists(GetConfigPath("config.json.template")))
                {
                    GenDefaultConfig("config.json.template");
                }
            }
            else
            {
                string jsonString = File.ReadAllText(GetConfigPath());
                ConfigData config_data = JsonConvert.DeserializeObject<ConfigData>(jsonString);
                if (config_data.host == null ||
                    config_data.ups == null)
                {
                    throw new Exception("Invalid config");
                }
                return new Config(config_data, GetConfigPath());
            }
            throw new Exception("Config file not found");
        }

        public static void GenDefaultConfig(string file_name)
        {
            ConfigData data = new ConfigData();
            string jsonString = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(GetConfigPath(file_name), jsonString);
        }
    }
}
