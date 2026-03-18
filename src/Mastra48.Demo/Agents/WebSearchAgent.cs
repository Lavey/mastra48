using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Mastra48.Demo.Services;

namespace Mastra48.Demo.Agents
{
    /// <summary>
    /// WebSearchAgent – performs web searches and returns formatted results.
    ///
    /// Strategy:
    ///   1. Try the free DuckDuckGo Instant Answer API (no key required).
    ///   2. Fall back to a rich built-in simulated result set when offline.
    ///
    /// All trace output is written in grey.
    /// </summary>
    public class WebSearchAgent : AgentBase
    {
        private readonly WebSearchService _searchService;

        public WebSearchAgent(WebSearchService searchService)
            : base("WebSearchAgent")
        {
            _searchService = searchService ?? throw new ArgumentNullException("searchService");
        }

        // ----------------------------------------------------------------
        // Main search entry point
        // ----------------------------------------------------------------

        /// <summary>
        /// Searches the web for <paramref name="query"/> and returns a formatted
        /// string containing a list of results and a short summary.
        /// </summary>
        public async Task<string> SearchAsync(string query)
        {
            Log($"Wyszukuję w sieci: \"{query}\"");
            LogStep("Próba połączenia z DuckDuckGo Instant Answer API...");

            List<SearchResult> results;
            string summary;

            try
            {
                (results, summary) = await _searchService.SearchAsync(query);
            }
            catch (Exception ex)
            {
                LogError($"Błąd wyszukiwania: {ex.Message}");
                return $"Nie udało się wykonać wyszukiwania: {ex.Message}";
            }

            LogStep($"Otrzymano {results.Count} wyników");

            if (results.Count == 0)
                return $"Brak wyników dla zapytania: \"{query}\"";

            return FormatResults(query, results, summary);
        }

        // ----------------------------------------------------------------
        // Formatting
        // ----------------------------------------------------------------

        private string FormatResults(string query, List<SearchResult> results, string summary)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"🔍 Wyniki wyszukiwania dla: \"{query}\"");
            sb.AppendLine(new string('-', 70));

            if (!string.IsNullOrEmpty(summary))
            {
                sb.AppendLine("📌 Podsumowanie:");
                sb.AppendLine($"   {summary}");
                sb.AppendLine();
            }

            sb.AppendLine($"📋 Znalezione strony ({results.Count}):");
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                sb.AppendLine($"\n  [{i + 1}] {r.Title}");
                sb.AppendLine($"      🔗 {r.Url}");
                if (!string.IsNullOrEmpty(r.Snippet))
                    sb.AppendLine($"      {r.Snippet}");
            }

            return sb.ToString().TrimEnd();
        }
    }
}
