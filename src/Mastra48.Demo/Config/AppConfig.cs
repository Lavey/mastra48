using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Mastra48.Demo.Config
{
    /// <summary>
    /// Application configuration loaded from config.json.
    /// API keys are never hardcoded – they are read from the config file at runtime.
    /// Uses JObject parsing for Mono compatibility (avoids System.Runtime.Serialization).
    /// </summary>
    public class AppConfig
    {
        /// <summary>Chat API key (leave empty to use simulated responses).</summary>
        public string ChatApiKey { get; set; }

        /// <summary>Chat model identifier, e.g. "gpt-4o-mini".</summary>
        public string ChatModel { get; set; }

        /// <summary>Log level: DEBUG, INFO, WARN, ERROR.</summary>
        public string LogLevel { get; set; }

        /// <summary>Directory used by FileAgent for demo file operations.</summary>
        public string DemoDataDirectory { get; set; }

        /// <summary>Returns true if a real Chat API key is configured.</summary>
        public bool HasChatKey
            => !string.IsNullOrWhiteSpace(ChatApiKey);

        // ----------------------------------------------------------------
        // Factory
        // ----------------------------------------------------------------

        /// <summary>
        /// Loads configuration from the given JSON file path.
        /// Falls back to defaults if the file does not exist.
        /// Uses JObject for Mono-compatible JSON parsing.
        /// </summary>
        public static AppConfig Load(string path = "config.json")
        {
            var defaults = new AppConfig
            {
                ChatApiKey = string.Empty,
                ChatModel = "gpt-4o-mini",
                LogLevel = "INFO",
                DemoDataDirectory = "demo_files"
            };

            if (!File.Exists(path))
                return defaults;

            try
            {
                var json = File.ReadAllText(path);
                var jObj = JObject.Parse(json);

                return new AppConfig
                {
                    ChatApiKey       = jObj["ChatApiKey"]?.ToString()       ?? defaults.ChatApiKey,
                    ChatModel        = jObj["ChatModel"]?.ToString()        ?? defaults.ChatModel,
                    LogLevel         = jObj["LogLevel"]?.ToString()         ?? defaults.LogLevel,
                    DemoDataDirectory= jObj["DemoDataDirectory"]?.ToString() ?? defaults.DemoDataDirectory
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Config] Warning: could not load config.json – " + ex.Message);
                return defaults;
            }
        }
    }
}

