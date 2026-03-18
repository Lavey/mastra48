using System;
using System.IO;
using System.Threading.Tasks;
using Mastra48.Demo.Config;
using Mastra48.Demo.Services;

namespace Mastra48.Demo.Agents
{
    /// <summary>
    /// ChatAgent – the main orchestrating agent.
    ///
    /// Responsibilities:
    ///   1. Drive the interactive console chat loop.
    ///   2. Detect the user's intent (keyword matching + optional Chat AI).
    ///   3. Delegate to the appropriate specialised agent:
    ///        FileAgent, WebSearchAgent, or DatabaseAgent.
    ///   4. Collect results and present the final answer in green.
    ///
    /// Supports built-in commands: help, demo1–demo4, exit.
    ///
    /// System prompt is loaded from the embedded resource Agents/Prompts/ChatAgent.md.
    /// </summary>
    public class ChatAgent : AgentBase
    {
        private readonly FileAgent _fileAgent;
        private readonly WebSearchAgent _webAgent;
        private readonly DatabaseAgent _dbAgent;
        private readonly ChatService _chat; // may be null
        private readonly AppConfig _config;
        private static readonly string _systemPrompt = ResourceLoader.Load("ChatAgent.md");

        // Separate system prompt for general conversational responses (not intent classification)
        private const string _generalResponseSystemPrompt =
            "Jesteś pomocnym asystentem wieloagentowego systemu Mastra48. " +
            "Odpowiedz użytkownikowi pomocnie po polsku. " +
            "Poinformuj o dostępnych możliwościach systemu: zapytania do bazy danych (zamówienia, " +
            "firmy, kontakty, oferty), operacje na plikach (szukaj, czytaj, zapisz, usuń) " +
            "oraz wyszukiwanie w internecie. Wpisz 'help' aby zobaczyć wszystkie komendy.";

        public ChatAgent(
            FileAgent fileAgent,
            WebSearchAgent webAgent,
            DatabaseAgent dbAgent,
            ChatService chat,
            AppConfig config)
            : base("ChatAgent")
        {
            _fileAgent = fileAgent;
            _webAgent  = webAgent;
            _dbAgent   = dbAgent;
            _chat      = chat;
            _config    = config;
        }

        // ----------------------------------------------------------------
        // Main chat loop
        // ----------------------------------------------------------------

        /// <summary>
        /// Starts the interactive console chat loop.
        /// Type 'exit' or 'quit' to stop.
        /// </summary>
        public async Task RunChatLoopAsync()
        {
            PrintBanner();
            Log("System agentów uruchomiony. Gotowy do pracy.");
            if (_chat != null)
                Log("Chat AI aktywne – rozpoznawanie intencji wspierane przez LLM.");
            else
                Log("Chat AI nieaktywne – używam rozpoznawania intencji opartego na słowach kluczowych.");

            Console.WriteLine();

            while (true)
            {
                PrintUserPrompt();
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                var trimmed = input.Trim();

                // ---- Built-in commands ----
                if (IsCommand(trimmed, "exit", "quit", "wyjdź", "koniec", "q"))
                {
                    Console.WriteLine("\nDo widzenia!");
                    break;
                }

                if (IsCommand(trimmed, "help", "pomoc", "?"))
                {
                    PrintHelp();
                    continue;
                }

                if (IsCommand(trimmed, "demo1"))
                {
                    await RunDemo1Async();
                    continue;
                }

                if (IsCommand(trimmed, "demo2"))
                {
                    await RunDemo2Async();
                    continue;
                }

                if (IsCommand(trimmed, "demo3"))
                {
                    await RunDemo3Async();
                    continue;
                }

                if (IsCommand(trimmed, "demo4"))
                {
                    await RunDemo4Async();
                    continue;
                }

                if (IsCommand(trimmed, "stats", "statystyki"))
                {
                    var stats = await _dbAgent.GetSummaryAsync();
                    PrintAssistantResponse(stats);
                    continue;
                }

                // ---- Route user query ----
                await HandleUserQueryAsync(trimmed);
            }
        }

        // ----------------------------------------------------------------
        // Intent detection + routing
        // ----------------------------------------------------------------

        private async Task HandleUserQueryAsync(string query)
        {
            Log($"Analizuję intencję użytkownika: \"{query}\"");

            var intent = await DetectIntentAsync(query);
            Log($"Wykryta intencja: {intent}");

            string response;

            switch (intent)
            {
                case "file":
                    response = await HandleFileIntentAsync(query);
                    break;

                case "web":
                    response = await _webAgent.SearchAsync(query);
                    break;

                case "database":
                    response = await _dbAgent.QueryAsync(query);
                    break;

                case "combined_db_file":
                    response = await HandleCombinedDbFileAsync(query);
                    break;

                default:
                    response = await GetGeneralResponseAsync(query);
                    break;
            }

            PrintAssistantResponse(response);
        }

        // ----------------------------------------------------------------
        // Intent detection
        // ----------------------------------------------------------------

        private async Task<string> DetectIntentAsync(string query)
        {
            // 1. Try Chat AI classification first (if available), using our system prompt
            if (_chat != null)
            {
                try
                {
                    Log("Klasyfikuję intencję przez Chat AI (systemprompt z ChatAgent.md)...");
                    // Use the loaded system prompt from ChatAgent.md for intent classification
                    var systemPrompt = !string.IsNullOrWhiteSpace(_systemPrompt)
                        ? _systemPrompt
                        : "Klasyfikuj zapytanie do jednej z kategorii: file, web, database, combined_db_file, general. Odpowiedz WYŁĄCZNIE nazwą kategorii.";
                    var result = await _chat.CompleteAsync(systemPrompt, query, maxTokens: 20);
                    var intent = (result ?? string.Empty).Trim().ToLower();
                    var valid = new[] { "file", "web", "database", "combined_db_file", "general" };
                    foreach (var v in valid)
                        if (intent.Contains(v)) { LogStep($"Chat AI zwrócił intencję: {v}"); return v; }
                    LogStep($"Chat AI zwrócił nieznaną intencję: {intent} – fallback do słownikowej.");
                }
                catch (Exception ex)
                {
                    LogWarning($"Chat AI niedostępny ({ex.Message}) – przełączam na klasyfikację słownikową.");
                }
            }

            // 2. Keyword-based fallback
            return ClassifyByKeywords(query);
        }

        private string ClassifyByKeywords(string query)
        {
            var q = query.ToLower();

            // Combined: DB + file save
            if ((ContainsAny(q, "zamówien", "ofert", "firm", "kontakt") && ContainsAny(q, "zapisz", "plik", "raport", "eksport")))
                return "combined_db_file";

            // File operations
            if (ContainsAny(q, "plik", "file", "otwórz", "odczytaj", "znajdź plik", "usuń", "skopiuj", "zawartość", "podsumuj plik", "listuj"))
                return "file";

            // Web search
            if (ContainsAny(q, "znajdź informacj", "wyszukaj", "google", "internet", "co to jest", "kim jest", "czym jest", "news", "szukaj"))
                return "web";

            // Database
            if (ContainsAny(q, "zamówien", "order", "firm", "compan", "kontakt", "contact", "ofert", "offer", "pokaż", "lista", "ile", "suma", "zestawienie", "raport", "baza", "sql"))
                return "database";

            return "general";
        }

        // ----------------------------------------------------------------
        // File intent handling
        // ----------------------------------------------------------------

        private async Task<string> HandleFileIntentAsync(string query)
        {
            var q = query.ToLower();

            // Summarise
            if (ContainsAny(q, "podsumuj", "streszcz", "summarize", "summary"))
            {
                var path = ExtractFilePath(query);
                if (path == null)
                    return "Nie podano ścieżki do pliku. Podaj pełną ścieżkę lub nazwę pliku, np. \"podsumuj plik demo_files/orders.txt\".";
                return await _fileAgent.SummarizeFileAsync(path);
            }

            // Read / open
            if (ContainsAny(q, "odczytaj", "otwórz", "read", "pokaż zawartość"))
            {
                var path = ExtractFilePath(query);
                if (path == null)
                    return "Nie podano ścieżki do pliku. Przykład: \"odczytaj plik demo_files/report.txt\"";
                return await _fileAgent.ReadFileAsync(path);
            }

            // Delete
            if (ContainsAny(q, "usuń", "delete", "skasuj"))
            {
                var path = ExtractFilePath(query);
                if (path == null)
                    return "Nie podano ścieżki do pliku do usunięcia.";
                return await _fileAgent.DeleteFileAsync(path);
            }

            // List
            if (ContainsAny(q, "listuj", "wylistuj", "pokaż pliki", "lista plików"))
            {
                return await _fileAgent.ListFilesAsync();
            }

            // Search (default for file intent)
            var searchPattern = ExtractSearchPattern(query) ?? "*.txt";
            return await _fileAgent.SearchFilesAsync(searchPattern);
        }

        // ----------------------------------------------------------------
        // Combined DB + File intent
        // ----------------------------------------------------------------

        private async Task<string> HandleCombinedDbFileAsync(string query)
        {
            Log("Scenariusz złożony: zapytanie do bazy + zapis do pliku");
            LogStep("Krok 1: DatabaseAgent – generowanie raportu");

            // Determine report type from query
            var reportType = "orders";
            var q = query.ToLower();
            if (ContainsAny(q, "firm")) reportType = "companies";
            else if (ContainsAny(q, "kontakt")) reportType = "contacts";
            else if (ContainsAny(q, "ofert")) reportType = "offers";

            // Generate file path
            var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var outputPath = Path.Combine(_config.DemoDataDirectory ?? "demo_files", $"raport_{reportType}_{ts}.txt");

            LogStep($"Krok 2: FileAgent – zapis do pliku: {outputPath}");

            return await _dbAgent.GenerateReportToFileAsync(reportType, outputPath);
        }

        // ----------------------------------------------------------------
        // General fallback response
        // ----------------------------------------------------------------

        private async Task<string> GetGeneralResponseAsync(string query)
        {
            // When LLM is available, use it with a dedicated conversational system prompt
            if (_chat != null)
            {
                try
                {
                    Log("Generuję odpowiedź ogólną przez Chat AI...");
                    var response = await _chat.CompleteAsync(_generalResponseSystemPrompt, query, maxTokens: 300);
                    if (!string.IsNullOrWhiteSpace(response))
                        return response;
                }
                catch (Exception ex)
                {
                    LogWarning($"Chat AI niedostępny ({ex.Message}) – używam odpowiedzi domyślnej.");
                }
            }

            return $"Rozumiem Twoje pytanie. Aby uzyskać najlepszą odpowiedź, możesz:\n" +
                   $"  • Zapytać o dane (np. \"Pokaż zamówienia z ostatniego miesiąca\")\n" +
                   $"  • Wyszukać informacje (np. \"Znajdź informacje o firmie Microsoft\")\n" +
                   $"  • Operować na plikach (np. \"Podsumuj plik demo_files/report.txt\")\n" +
                   $"  • Wpisz 'help' aby zobaczyć wszystkie możliwości.\n\n" +
                   $"Twoje zapytanie: \"{query}\" – nie rozpoznałem jednoznacznej intencji.";
        }

        // ----------------------------------------------------------------
        // Demo scenarios
        // ----------------------------------------------------------------

        /// <summary>Demo 1: Find a file and summarise its content.</summary>
        private async Task RunDemo1Async()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine(" DEMO 1: Wyszukanie pliku i podsumowanie jego zawartości");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.ResetColor();

            // Step 1: Create a demo file to work with
            Log("Przygotowuję plik demonstracyjny...");
            var demoPath = Path.Combine(_config.DemoDataDirectory ?? "demo_files", "sample_report.txt");
            var demoContent = "# Raport Miesięczny – Styczeń 2025\n\n" +
                              "## Podsumowanie Sprzedaży\n" +
                              "Miesiąc styczeń 2025 przyniósł wzrost sprzedaży o 12% w stosunku do analogicznego okresu roku poprzedniego.\n" +
                              "Główne rynki: Polska, Niemcy, Czechy.\n\n" +
                              "## Kluczowe wskaźniki\n" +
                              "- Przychody: 1 250 000 PLN\n" +
                              "- Nowi klienci: 15\n" +
                              "- Zamówienia: 87\n" +
                              "- Średnia wartość zamówienia: 14 368 PLN\n\n" +
                              "## Produkty Top 3\n" +
                              "1. System ERP – 45 licencji\n" +
                              "2. Usługi Cloud – 23 kontrakty\n" +
                              "3. Szkolenia IT – 19 szkoleń\n\n" +
                              "## Prognoza Luty 2025\n" +
                              "Oczekujemy wzrostu o 8–10% dzięki planowanej kampanii marketingowej.";

            await _fileAgent.WriteFileAsync(demoPath, demoContent, requireConfirmation: false);

            // Step 2: Search for the file
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\nUżytkownik: Znajdź plik o nazwie 'sample' i podsumuj jego zawartość");
            Console.ResetColor();

            var searchResult = await _fileAgent.SearchFilesAsync("sample");
            Log($"Znalezione pliki:\n{searchResult}");

            // Step 3: Summarise
            var summary = await _fileAgent.SummarizeFileAsync(demoPath);
            PrintAssistantResponse(summary);
        }

        /// <summary>Demo 2: Database query – orders from the last month.</summary>
        private async Task RunDemo2Async()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine(" DEMO 2: Zapytanie do bazy – zamówienia z ostatniego miesiąca");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\nUżytkownik: Pokaż zamówienia z ostatniego miesiąca");
            Console.ResetColor();

            var result = await _dbAgent.QueryAsync("Pokaż zamówienia z ostatniego miesiąca");
            PrintAssistantResponse(result);
        }

        /// <summary>Demo 3: Web search – company info.</summary>
        private async Task RunDemo3Async()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine(" DEMO 3: Wyszukiwanie w internecie – informacje o firmie");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\nUżytkownik: Znajdź informacje o firmie OpenAI");
            Console.ResetColor();

            var result = await _webAgent.SearchAsync("OpenAI");
            PrintAssistantResponse(result);
        }

        /// <summary>Demo 4: Combined – fetch data from DB and save to file.</summary>
        private async Task RunDemo4Async()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine(" DEMO 4: Scenariusz złożony – pobranie danych + zapis do pliku");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\nUżytkownik: Pobierz dane zamówień i zapisz raport do pliku");
            Console.ResetColor();

            var result = await HandleCombinedDbFileAsync("zamówienia zapisz do pliku raport");
            PrintAssistantResponse(result);
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        private bool IsCommand(string input, params string[] commands)
        {
            foreach (var cmd in commands)
                if (input.Equals(cmd, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private bool ContainsAny(string text, params string[] keywords)
        {
            foreach (var kw in keywords)
                if (text.Contains(kw)) return true;
            return false;
        }

        /// <summary>
        /// Tries to extract a file path from the user's query.
        /// Looks for a token that looks like a path (contains '/' or '\' or ends in common extensions).
        /// </summary>
        private string ExtractFilePath(string query)
        {
            var tokens = query.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (token.Contains("/") || token.Contains("\\") ||
                    token.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                    token.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ||
                    token.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                    token.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                {
                    return token;
                }
            }
            return null;
        }

        /// <summary>Extracts a search pattern from a query like "znajdź plik raport".</summary>
        private string ExtractSearchPattern(string query)
        {
            var keywords = new[] { "plik", "file", "znajdź", "szukaj", "search", "wzorzec" };
            var tokens = query.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < tokens.Length; i++)
            {
                foreach (var kw in keywords)
                {
                    if (tokens[i].Equals(kw, StringComparison.OrdinalIgnoreCase) && i + 1 < tokens.Length)
                        return tokens[i + 1];
                }
            }
            // Return last meaningful token as pattern
            if (tokens.Length > 0) return tokens[tokens.Length - 1];
            return null;
        }

        // ----------------------------------------------------------------
        // UI helpers
        // ----------------------------------------------------------------

        private void PrintBanner()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║           MASTRA48 – SYSTEM AGENTÓW DEMO                    ║");
            Console.WriteLine("║  inspirowany https://github.com/openai                      ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║  Agenci: Chat | File | WebSearch | Database                  ║");
            Console.WriteLine("║  Wpisz 'help' po listę komend  •  'exit' aby wyjść          ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        private void PrintHelp()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n📖 Dostępne komendy i przykłady zapytań:");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("  Komendy systemowe:");
            Console.WriteLine("    help / pomoc     – ta pomoc");
            Console.WriteLine("    stats            – statystyki bazy danych");
            Console.WriteLine("    demo1            – demo: wyszukaj plik i podsumuj");
            Console.WriteLine("    demo2            – demo: zapytanie do bazy (zamówienia)");
            Console.WriteLine("    demo3            – demo: wyszukiwanie w internecie");
            Console.WriteLine("    demo4            – demo: baza + zapis do pliku");
            Console.WriteLine("    exit / wyjdź     – zakończ program");
            Console.WriteLine();
            Console.WriteLine("  📁 Operacje na plikach:");
            Console.WriteLine("    Znajdź plik raport");
            Console.WriteLine("    Odczytaj plik demo_files/sample_report.txt");
            Console.WriteLine("    Podsumuj plik demo_files/sample_report.txt");
            Console.WriteLine("    Usuń plik demo_files/test.txt");
            Console.WriteLine("    Listuj pliki");
            Console.WriteLine();
            Console.WriteLine("  📊 Zapytania do bazy:");
            Console.WriteLine("    Pokaż zamówienia z ostatniego miesiąca");
            Console.WriteLine("    Lista firm z branży IT");
            Console.WriteLine("    Pokaż kontakty na stanowisku dyrektor");
            Console.WriteLine("    Oferty ze statusem Accepted");
            Console.WriteLine("    Ile wynosi suma zamówień?");
            Console.WriteLine();
            Console.WriteLine("  🔍 Wyszukiwanie w internecie:");
            Console.WriteLine("    Znajdź informacje o firmie Microsoft");
            Console.WriteLine("    Szukaj: sztuczna inteligencja");
            Console.WriteLine("    Co to jest OpenAI?");
            Console.WriteLine();
            Console.WriteLine("  🔗 Scenariusze złożone:");
            Console.WriteLine("    Pobierz dane zamówień i zapisz do pliku");
            Console.WriteLine("    Wygeneruj raport firm i wyeksportuj");
            Console.WriteLine();
        }
    }
}
