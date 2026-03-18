using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mastra48.Demo.Services
{
    /// <summary>
    /// Thin wrapper around the Mistral AI REST API.
    /// Used when a valid API key is provided in config.json.
    /// API key is never hardcoded – it is always injected from configuration.
    /// Uses WebRequest/WebClient for compatibility with Mono on .NET Framework 4.8.
    /// </summary>
    public class MistralService
    {
        private readonly string _apiKey;
        private readonly string _model;
        private const string ApiBaseUrl = "https://api.mistral.ai/v1/chat/completions";

        public MistralService(string apiKey, string model = "mistral-small-latest")
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be empty.", "apiKey");
            _apiKey = apiKey;
            _model  = model ?? "mistral-small-latest";
        }

        // ----------------------------------------------------------------
        // Chat completion
        // ----------------------------------------------------------------

        /// <summary>
        /// Sends a list of messages to the Mistral chat endpoint and returns
        /// the assistant's reply text.
        /// </summary>
        public Task<string> ChatAsync(List<MistralMessage> messages, int maxTokens = 1024)
        {
            return Task.Run<string>(() =>
            {
                var requestBody = new
                {
                    model = _model,
                    messages = messages,
                    max_tokens = maxTokens,
                    temperature = 0.3
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var bytes = Encoding.UTF8.GetBytes(json);

                var request = (HttpWebRequest)WebRequest.Create(ApiBaseUrl);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = bytes.Length;
                request.Headers.Add("Authorization", "Bearer " + _apiKey);
                request.Timeout = 30000;

                using (var stream = request.GetRequestStream())
                    stream.Write(bytes, 0, bytes.Length);

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    var responseJson = reader.ReadToEnd();
                    var jObj = JObject.Parse(responseJson);
                    return jObj["choices"]?[0]?["message"]?["content"]?.ToString() ?? string.Empty;
                }
            });
        }

        // ----------------------------------------------------------------
        // Convenience helpers
        // ----------------------------------------------------------------

        /// <summary>Single-turn completion with a system prompt and user message.</summary>
        public Task<string> CompleteAsync(string systemPrompt, string userMessage, int maxTokens = 1024)
        {
            var messages = new List<MistralMessage>
            {
                new MistralMessage { Role = "system", Content = systemPrompt },
                new MistralMessage { Role = "user",   Content = userMessage }
            };
            return ChatAsync(messages, maxTokens);
        }

        /// <summary>
        /// Asks Mistral to classify the intent of a user message.
        /// Returns one of: "file", "web", "database", "combined_db_file", "general".
        /// </summary>
        public async Task<string> ClassifyIntentAsync(string userMessage)
        {
            var system = "You are an intent classifier for a multi-agent assistant system.\n" +
                         "Classify the user's message into exactly ONE of these categories:\n" +
                         "- file       : file operations (search, read, summarize, delete files)\n" +
                         "- web        : web search or questions about external information\n" +
                         "- database   : queries about orders, companies, contacts, or offers\n" +
                         "- combined_db_file : querying the database AND saving results to a file\n" +
                         "- general    : greetings, help, or unclear intent\n\n" +
                         "Respond with ONLY the category name, nothing else.";

            try
            {
                var result = await CompleteAsync(system, userMessage, maxTokens: 20);
                var intent = result.Trim().ToLower();
                var valid = new[] { "file", "web", "database", "combined_db_file", "general" };
                foreach (var v in valid)
                    if (intent.Contains(v)) return v;
                return "general";
            }
            catch
            {
                return "general";
            }
        }

        /// <summary>
        /// Asks Mistral to generate a short AI-powered summary of the given text.
        /// </summary>
        public async Task<string> SummarizeAsync(string text, int maxChars = 3000)
        {
            var truncated = text.Length > maxChars ? text.Substring(0, maxChars) + "\n[...]" : text;
            var system = "You are a helpful assistant. Summarize the provided text concisely in Polish. Keep it to 3-5 sentences.";
            return await CompleteAsync(system, truncated);
        }
    }

    /// <summary>A single message in a Mistral chat conversation.</summary>
    public class MistralMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }
}
