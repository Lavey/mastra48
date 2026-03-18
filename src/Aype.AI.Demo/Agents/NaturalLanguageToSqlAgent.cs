using System;
using System.Text;
using System.Threading.Tasks;
using Aype.AI.Demo.Config;
using Aype.AI.Demo.Services;

namespace Aype.AI.Demo.Agents
{
    /// <summary>
    /// NaturalLanguageToSqlAgent – translates natural language queries into SQL SELECT statements.
    ///
    /// When ChatService (LLM) is available, uses AI to generate accurate SQL based on the
    /// database schema defined in the embedded system prompt.
    ///
    /// Falls back to keyword-based SQL generation when LLM is unavailable or returns an
    /// empty response.
    ///
    /// System prompt is loaded from the embedded resource Agents/Prompts/NaturalLanguageToSqlAgent.md.
    /// </summary>
    public class NaturalLanguageToSqlAgent : AgentBase
    {
        private readonly ChatService _chat; // may be null
        private static readonly string _systemPrompt = ResourceLoader.Load("NaturalLanguageToSqlAgent.md");

        public NaturalLanguageToSqlAgent(ChatService chat = null)
            : base("NL2SQLAgent")
        {
            _chat = chat;
        }

        // ----------------------------------------------------------------
        // Main translation entry point
        // ----------------------------------------------------------------

        /// <summary>
        /// Translates a natural-language <paramref name="query"/> into a SQL SELECT statement.
        /// Uses LLM when available; falls back to keyword-based generation.
        /// </summary>
        public async Task<string> TranslateAsync(string query)
        {
            Log($"Tłumaczę na SQL: \"{query}\"");

            if (_chat != null)
            {
                try
                {
                    LogStep("Używam LLM do generowania SQL...");
                    var sql = await _chat.CompleteAsync(_systemPrompt, query, maxTokens: 200);
                    sql = CleanSql(sql);

                    if (!string.IsNullOrWhiteSpace(sql))
                    {
                        LogStep($"LLM wygenerował SQL: {sql}");
                        return sql;
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"LLM niedostępny ({ex.Message}) – używam fallback.");
                }
            }

            // Fallback: keyword-based SQL generation
            var fallback = GenerateFallbackSql(query);
            LogStep($"Keyword fallback SQL: {fallback}");
            return fallback;
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        /// <summary>
        /// Strips markdown code fences (```sql ... ```) that the LLM may add
        /// and trims surrounding whitespace.
        /// </summary>
        private static string CleanSql(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var lines = raw.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                // Skip markdown code fence lines
                if (trimmed.StartsWith("```"))
                    continue;
                sb.AppendLine(trimmed);
            }

            return sb.ToString().Trim();
        }

        /// <summary>Keyword-based fallback that generates a basic SQL query.</summary>
        private string GenerateFallbackSql(string query)
        {
            var q = query.ToLower();

            // Orders
            if (ContainsAny(q, "zamówien", "zamówień", "order"))
            {
                if (ContainsAny(q, "suma", "łączna", "total"))
                    return "SELECT SUM(Amount) AS TotalAmount FROM Orders";
                if (ContainsAny(q, "ostatni miesiąc", "last month"))
                    return "SELECT * FROM Orders WHERE Date >= DATEADD(day, -30, GETDATE()) ORDER BY Date DESC";
                if (ContainsAny(q, "ostatni tydzień", "last week"))
                    return "SELECT * FROM Orders WHERE Date >= DATEADD(day, -7, GETDATE()) ORDER BY Date DESC";
                if (ContainsAny(q, "pending", "oczekując"))
                    return "SELECT * FROM Orders WHERE Status = 'Pending' ORDER BY Date DESC";
                if (ContainsAny(q, "delivered", "dostarczon", "zrealizowa"))
                    return "SELECT * FROM Orders WHERE Status = 'Delivered' ORDER BY Date DESC";
                if (ContainsAny(q, "cancelled", "anulowa"))
                    return "SELECT * FROM Orders WHERE Status = 'Cancelled' ORDER BY Date DESC";
                if (ContainsAny(q, "duż", "wysok", "wielk", "powyżej"))
                    return "SELECT * FROM Orders WHERE Amount > 10000 ORDER BY Amount DESC";
                return "SELECT * FROM Orders ORDER BY Date DESC";
            }

            // Companies
            if (ContainsAny(q, "firm", "compan", "przedsiębiorstw"))
            {
                if (ContainsAny(q, "warszawa"))
                    return "SELECT * FROM Companies WHERE City = 'Warszawa' ORDER BY Name";
                if (ContainsAny(q, "kraków", "krakow"))
                    return "SELECT * FROM Companies WHERE City = 'Kraków' ORDER BY Name";
                if (ContainsAny(q, " it", "informatycz", "technolog"))
                    return "SELECT * FROM Companies WHERE Industry = 'IT' ORDER BY Name";
                return "SELECT * FROM Companies ORDER BY Name";
            }

            // Contacts
            if (ContainsAny(q, "kontakt", "contact", "osob", "pracownik"))
            {
                if (ContainsAny(q, "dyrektor", "director"))
                    return "SELECT * FROM Contacts WHERE Position LIKE '%Dyrektor%' ORDER BY LastName";
                if (ContainsAny(q, "kierownik", "manager"))
                    return "SELECT * FROM Contacts WHERE Position LIKE '%Kierownik%' ORDER BY LastName";
                if (ContainsAny(q, "prezes", "ceo"))
                    return "SELECT * FROM Contacts WHERE Position LIKE '%Prezes%' ORDER BY LastName";
                return "SELECT * FROM Contacts ORDER BY LastName";
            }

            // Offers
            if (ContainsAny(q, "ofert", "offer", "propozycj"))
            {
                if (ContainsAny(q, "przyjęt", "zaakceptowa", "accepted"))
                    return "SELECT * FROM Offers WHERE Status = 'Accepted' ORDER BY ValidUntil";
                if (ContainsAny(q, "odrzucon", "rejected"))
                    return "SELECT * FROM Offers WHERE Status = 'Rejected' ORDER BY ValidUntil";
                if (ContainsAny(q, "wysłan", "sent"))
                    return "SELECT * FROM Offers WHERE Status = 'Sent' ORDER BY ValidUntil";
                return "SELECT * FROM Offers ORDER BY ValidUntil";
            }

            return "SELECT * FROM Orders ORDER BY Date DESC";
        }

        private bool ContainsAny(string text, params string[] keywords)
        {
            foreach (var kw in keywords)
                if (text.Contains(kw)) return true;
            return false;
        }
    }
}
