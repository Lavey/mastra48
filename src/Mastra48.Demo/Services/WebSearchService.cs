using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Mastra48.Demo.Services
{
    /// <summary>
    /// Provides web-search capabilities for the WebSearchAgent.
    /// Uses the free DuckDuckGo Instant Answer API when possible; falls back to
    /// a built-in simulated result set so the demo works fully offline.
    /// Uses WebClient/WebRequest for compatibility with Mono on .NET Framework 4.8.
    /// </summary>
    public class WebSearchService
    {
        // ----------------------------------------------------------------
        // Main search entry point
        // ----------------------------------------------------------------

        /// <summary>
        /// Performs a web search for the given query.
        /// Returns a list of SearchResult objects and a short summary.
        /// </summary>
        public async Task<(List<SearchResult> results, string summary)> SearchAsync(string query)
        {
            // Try DuckDuckGo first (free, no API key required)
            try
            {
                var ddg = await SearchDuckDuckGoAsync(query);
                if (ddg.results != null && ddg.results.Count > 0)
                    return ddg;
            }
            catch
            {
                // Fall through to simulation on any network error
            }

            // Fallback: simulated results
            return SimulatedSearch(query);
        }

        // ----------------------------------------------------------------
        // DuckDuckGo Instant Answer API
        // ----------------------------------------------------------------

        private Task<(List<SearchResult> results, string summary)> SearchDuckDuckGoAsync(string query)
        {
            return Task.Run<(List<SearchResult> results, string summary)>(() =>
            {
                var url = "https://api.duckduckgo.com/?q=" + Uri.EscapeDataString(query) + "&format=json&no_html=1&skip_disambig=1";

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = 8000;
                request.UserAgent = "Mastra48Demo/1.0";

                string response;
                using (var webResponse = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                {
                    response = reader.ReadToEnd();
                }

                var json = JObject.Parse(response);
                var results = new List<SearchResult>();

                // Abstract (featured snippet)
                var abstractText = json["AbstractText"]?.ToString();
                var abstractUrl  = json["AbstractURL"]?.ToString();
                if (!string.IsNullOrEmpty(abstractText))
                {
                    results.Add(new SearchResult
                    {
                        Title   = json["Heading"]?.ToString() ?? query,
                        Url     = abstractUrl ?? "https://duckduckgo.com",
                        Snippet = abstractText
                    });
                }

                // Related topics
                var topics = json["RelatedTopics"] as JArray;
                if (topics != null)
                {
                    foreach (var topic in topics)
                    {
                        var text = topic["Text"]?.ToString();
                        var url2 = topic["FirstURL"]?.ToString();
                        if (!string.IsNullOrEmpty(text))
                        {
                            results.Add(new SearchResult
                            {
                                Title   = text.Length > 60 ? text.Substring(0, 60) + "..." : text,
                                Url     = url2 ?? "https://duckduckgo.com",
                                Snippet = text
                            });
                        }
                        if (results.Count >= 5) break;
                    }
                }

                if (results.Count == 0)
                    return (results, string.Empty);

                var summary = string.IsNullOrEmpty(abstractText)
                    ? "Znaleziono " + results.Count + " wyników dla: \"" + query + "\""
                    : abstractText;

                return (results, summary);
            });
        }

        // ----------------------------------------------------------------
        // Simulated search (offline fallback)
        // ----------------------------------------------------------------

        private (List<SearchResult> results, string summary) SimulatedSearch(string query)
        {
            var q = query.ToLower();
            var results = new List<SearchResult>();
            string summary;

            // Keyword-based simulated result generation
            if (ContainsAny(q, "microsoft"))
            {
                results.Add(new SearchResult { Title = "Microsoft Corporation – Wikipedia", Url = "https://en.wikipedia.org/wiki/Microsoft", Snippet = "Microsoft Corporation to wielonarodowe przedsiębiorstwo technologiczne z siedzibą w Redmond, WA. Założona w 1975 przez Billa Gatesa i Paula Allena." });
                results.Add(new SearchResult { Title = "Microsoft oficjalna strona", Url = "https://www.microsoft.com", Snippet = "Produkty i usługi Microsoft: Windows, Azure, Office 365, Teams, Dynamics 365." });
                summary = "Microsoft Corporation to jeden z największych producentów oprogramowania na świecie, znany z systemów Windows i pakietu Office.";
            }
            else if (ContainsAny(q, "google"))
            {
                results.Add(new SearchResult { Title = "Google LLC – Wikipedia", Url = "https://en.wikipedia.org/wiki/Google", Snippet = "Google to wielonarodowe przedsiębiorstwo technologiczne specjalizujące się w usługach internetowych." });
                results.Add(new SearchResult { Title = "Google oficjalna strona", Url = "https://www.google.com", Snippet = "Wyszukiwarka Google, Google Maps, Gmail, YouTube i inne produkty." });
                summary = "Google LLC to firma technologiczna z siedzibą w Mountain View, CA, znana z wyszukiwarki internetowej i platformy reklamowej.";
            }
            else if (ContainsAny(q, "ai", "sztuczna inteligencja", "artificial intelligence"))
            {
                results.Add(new SearchResult { Title = "Sztuczna inteligencja – Wikipedia", Url = "https://pl.wikipedia.org/wiki/Sztuczna_inteligencja", Snippet = "Sztuczna inteligencja (AI) – dziedzina informatyki zajmująca się tworzeniem maszyn zdolnych do wykonywania zadań wymagających inteligencji." });
                results.Add(new SearchResult { Title = "ChatGPT – OpenAI", Url = "https://chat.openai.com", Snippet = "ChatGPT to duży model językowy (LLM) opracowany przez OpenAI, dostępny jako chatbot." });
                results.Add(new SearchResult { Title = "Mistral AI", Url = "https://mistral.ai", Snippet = "Mistral AI – europejska firma AI tworząca modele językowe open-source." });
                summary = "Sztuczna inteligencja to dynamicznie rozwijająca się dziedzina – kluczowi gracze to OpenAI, Google DeepMind, Anthropic i Mistral AI.";
            }
            else if (ContainsAny(q, "mistral"))
            {
                results.Add(new SearchResult { Title = "Mistral AI – oficjalna strona", Url = "https://mistral.ai", Snippet = "Mistral AI to francuska firma AI oferująca modele Mistral 7B, Mixtral 8x7B, Mistral Large." });
                results.Add(new SearchResult { Title = "Mistral AI – Wikipedia", Url = "https://en.wikipedia.org/wiki/Mistral_AI", Snippet = "Mistral AI SAS – firma założona w 2023 r. w Paryżu przez byłych pracowników Google DeepMind i Meta." });
                results.Add(new SearchResult { Title = "Mistral AI – GitHub", Url = "https://github.com/mistralai", Snippet = "Oficjalne repozytorium Mistral AI na GitHub – modele, narzędzia i dokumentacja." });
                summary = "Mistral AI to europejska firma AI z siedzibą w Paryżu, specjalizująca się w otwartych modelach językowych wysokiej wydajności.";
            }
            else if (ContainsAny(q, ".net", "dotnet", "csharp", "c#"))
            {
                results.Add(new SearchResult { Title = ".NET – Microsoft Docs", Url = "https://docs.microsoft.com/dotnet", Snippet = ".NET to platforma deweloperska open-source firmy Microsoft dla systemów Windows, Linux i macOS." });
                results.Add(new SearchResult { Title = "C# Programming Guide", Url = "https://docs.microsoft.com/csharp", Snippet = "C# (C-sharp) to nowoczesny, wieloparadygmatowy język programowania opracowany przez Microsoft." });
                summary = ".NET Framework i .NET Core to platformy deweloperskie Microsoft. C# to wiodący język ekosystemu .NET.";
            }
            else
            {
                // Generic fallback
                results.Add(new SearchResult { Title = "Wyniki dla: " + query, Url = "https://duckduckgo.com/?q=" + Uri.EscapeDataString(query), Snippet = "Kliknij link, aby zobaczyć wyniki wyszukiwania dla frazy \"" + query + "\"." });
                results.Add(new SearchResult { Title = "Wikipedia – " + query, Url = "https://pl.wikipedia.org/wiki/" + Uri.EscapeDataString(query), Snippet = "Artykuł encyklopedyczny na temat: " + query + "." });
                results.Add(new SearchResult { Title = "Najnowsze informacje o: " + query, Url = "https://news.google.com/search?q=" + Uri.EscapeDataString(query), Snippet = "Najnowsze wiadomości i artykuły dotyczące tematu \"" + query + "\"." });
                summary = "(Wyniki symulowane) Wyszukiwano: \"" + query + "\". Uruchom aplikację z dostępem do sieci, aby pobrać prawdziwe wyniki z DuckDuckGo.";
            }

            return (results, summary);
        }

        // ----------------------------------------------------------------
        // Helper
        // ----------------------------------------------------------------

        private static bool ContainsAny(string text, params string[] keywords)
        {
            foreach (var kw in keywords)
                if (text.Contains(kw)) return true;
            return false;
        }
    }

    /// <summary>Represents a single web search result.</summary>
    public class SearchResult
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Snippet { get; set; }

        public override string ToString()
            => "  " + Title + "\n     " + Url + "\n     " + Snippet;
    }
}
