using System;
using System.Threading.Tasks;
using Mastra48.Demo.Agents;
using Mastra48.Demo.Config;
using Mastra48.Demo.Services;

namespace Mastra48.Demo
{
    /// <summary>
    /// Entry point for the Mastra48 Demo console application.
    ///
    /// Architecture:
    ///   ChatAgent (orchestrator)
    ///     ├── FileAgent     – file read / write / search / summarise
    ///     ├── WebSearchAgent – web search (DuckDuckGo or simulated)
    ///     └── DatabaseAgent  – natural language → SQL → in-memory results
    ///
    /// Configuration is loaded from config.json (next to the executable).
    /// Chat AI integration is optional and controlled by the API key in config.json.
    /// </summary>
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            // ----------------------------------------------------------------
            // 1. Load configuration
            // ----------------------------------------------------------------
            var config = AppConfig.Load("config.json");

            // ----------------------------------------------------------------
            // 2. Initialise services
            // ----------------------------------------------------------------

            // Optional Chat AI service (null when no API key provided)
            ChatService chat = null;
            if (config.HasChatKey)
            {
                try
                {
                    chat = new ChatService(config.ChatApiKey, config.ChatModel);
                    WriteGray("[Main] Chat AI zainicjowany.");
                }
                catch (Exception ex)
                {
                    WriteGray($"[Main] Nie można zainicjować Chat AI: {ex.Message}");
                }
            }
            else
            {
                WriteGray("[Main] Klucz Chat AI nie jest skonfigurowany – tryb symulacji aktywny.");
            }

            var fileService   = new FileService(config.DemoDataDirectory ?? "demo_files");
            var searchService = new WebSearchService();
            var dbService     = new DatabaseService();

            WriteGray($"[Main] Baza danych zainicjalizowana:");
            WriteGray($"         Zamówienia: {DatabaseService.Orders.Count}, Firmy: {DatabaseService.Companies.Count}");
            WriteGray($"         Kontakty: {DatabaseService.Contacts.Count}, Oferty: {DatabaseService.Offers.Count}");

            // ----------------------------------------------------------------
            // 3. Initialise agents (inject dependencies)
            // ----------------------------------------------------------------

            // FileAgent (can use Chat AI for AI-powered summaries)
            var fileAgent = new FileAgent(fileService, chat);

            // WebSearchAgent
            var webAgent = new WebSearchAgent(searchService);

            // DatabaseAgent (collaborates with FileAgent for DB→File scenarios)
            var dbAgent = new DatabaseAgent(dbService, fileAgent);

            // ChatAgent (main orchestrator)
            var chatAgent = new ChatAgent(fileAgent, webAgent, dbAgent, chat, config);

            // ----------------------------------------------------------------
            // 4. Start chat loop
            // ----------------------------------------------------------------
            try
            {
                await chatAgent.RunChatLoopAsync();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[BŁĄD KRYTYCZNY] {ex.Message}");
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        private static void WriteGray(string message)
        {
            var saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(message);
            Console.ForegroundColor = saved;
        }
    }
}
