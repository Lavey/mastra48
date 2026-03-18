using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Mastra48.Demo.Config;
using Mastra48.Demo.Services;

namespace Mastra48.Demo.Agents
{
    /// <summary>
    /// DatabaseAgent – translates natural language to SQL (shown for transparency),
    /// executes it against the in-memory data store, and returns formatted results.
    ///
    /// SQL generation is delegated to NaturalLanguageToSqlAgent which uses an LLM
    /// when available, falling back to keyword-based translation.
    ///
    /// Can collaborate with FileAgent to save query results to a file.
    ///
    /// System prompt is loaded from the embedded resource Agents/Prompts/DatabaseAgent.md.
    /// Trace output is written in grey.
    /// </summary>
    public class DatabaseAgent : AgentBase
    {
        private readonly DatabaseService _db;
        private readonly FileAgent _fileAgent;               // optional collaborator
        private readonly NaturalLanguageToSqlAgent _nl2sql;  // SQL generation sub-agent
        private static readonly string _systemPrompt = ResourceLoader.Load("DatabaseAgent.md");

        public DatabaseAgent(DatabaseService db, FileAgent fileAgent = null, NaturalLanguageToSqlAgent nl2sql = null)
            : base("DatabaseAgent")
        {
            _db      = db ?? throw new ArgumentNullException("db");
            _fileAgent = fileAgent;
            _nl2sql  = nl2sql;
        }

        // ----------------------------------------------------------------
        // Main query entry point
        // ----------------------------------------------------------------

        /// <summary>
        /// Parses a natural-language <paramref name="query"/>, delegates SQL generation to
        /// NaturalLanguageToSqlAgent (LLM-powered), shows the generated SQL, executes the
        /// query against in-memory data, and returns a formatted result.
        /// </summary>
        public async Task<string> QueryAsync(string query)
        {
            Log($"Analizuję zapytanie: \"{query}\"");
            LogStep("Delegowanie tłumaczenia NL→SQL do NaturalLanguageToSqlAgent...");

            // Step 1: Generate SQL via dedicated sub-agent (uses LLM when available)
            string displaySql;
            if (_nl2sql != null)
            {
                displaySql = await _nl2sql.TranslateAsync(query);
            }
            else
            {
                // Fallback when no NL2SQL agent is wired up
                LogStep("NaturalLanguageToSqlAgent niedostępny – używam fallback DatabaseService.");
                displaySql = null;
            }

            // Step 2: Execute against in-memory data via DatabaseService (LINQ-based)
            LogStep("Wykonuję zapytanie na danych in-memory...");
            string executedSql;
            string results;

            try
            {
                (executedSql, results) = _db.ExecuteNaturalQuery(query);
            }
            catch (Exception ex)
            {
                LogError($"Błąd wykonania zapytania: {ex.Message}");
                return $"Błąd: {ex.Message}";
            }

            // Use the LLM-generated SQL for display when available; otherwise show the keyword-based SQL
            var sqlToShow = !string.IsNullOrWhiteSpace(displaySql) ? displaySql : executedSql;
            LogStep($"SQL (wyświetlany): {sqlToShow}");
            Log("Zapytanie zakończone");

            var sb = new StringBuilder();
            sb.AppendLine($"📊 Zapytanie do bazy danych");
            sb.AppendLine($"SQL: {sqlToShow}");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine(results);

            return sb.ToString().TrimEnd();
        }

        // ----------------------------------------------------------------
        // Report generation (DB → File collaboration)
        // ----------------------------------------------------------------

        /// <summary>
        /// Generates a full report for the given <paramref name="reportType"/>
        /// and saves it to <paramref name="outputPath"/> using the FileAgent.
        ///
        /// Demonstrates Database Agent → File Agent collaboration.
        /// </summary>
        public async Task<string> GenerateReportToFileAsync(string reportType, string outputPath)
        {
            Log($"Generuję raport: {reportType}");
            LogStep("Pobieranie danych z bazy...");

            string reportContent;
            try
            {
                reportContent = _db.GenerateReport(reportType);
            }
            catch (Exception ex)
            {
                LogError($"Błąd generowania raportu: {ex.Message}");
                return $"Błąd generowania raportu: {ex.Message}";
            }

            LogStep($"Raport zawiera {reportContent.Length} znaków");

            if (_fileAgent == null)
            {
                // No FileAgent – return report as text
                Log("FileAgent niedostępny – zwracam raport jako tekst");
                return $"📊 Raport ({reportType}):\n{reportContent}";
            }

            // Delegate saving to FileAgent
            Log($"Delegowanie zapisu do FileAgent → {outputPath}");
            var saveResult = await _fileAgent.WriteFileAsync(outputPath, reportContent, requireConfirmation: false);

            return $"📊 Raport \"{reportType}\" wygenerowany.\n{saveResult}\n\nPodgląd:\n{reportContent.Substring(0, Math.Min(500, reportContent.Length))}...";
        }

        // ----------------------------------------------------------------
        // Summary statistics
        // ----------------------------------------------------------------

        /// <summary>Returns high-level summary statistics for the database.</summary>
        public Task<string> GetSummaryAsync()
        {
            Log("Generuję podsumowanie bazy danych...");

            var sb = new StringBuilder();
            sb.AppendLine("📊 Podsumowanie bazy danych:");
            sb.AppendLine(new string('-', 50));
            sb.AppendLine($"  Zamówienia (Orders)  : {DatabaseService.Orders.Count} rekordów");
            sb.AppendLine($"  Firmy (Companies)    : {DatabaseService.Companies.Count} rekordów");
            sb.AppendLine($"  Kontakty (Contacts)  : {DatabaseService.Contacts.Count} rekordów");
            sb.AppendLine($"  Oferty (Offers)      : {DatabaseService.Offers.Count} rekordów");

            // Quick stats
            var totalOrderValue = 0m;
            foreach (var o in DatabaseService.Orders) totalOrderValue += o.Amount;
            var totalOfferValue = 0m;
            foreach (var o in DatabaseService.Offers) totalOfferValue += o.Value;

            sb.AppendLine(new string('-', 50));
            sb.AppendLine($"  Łączna wartość zamówień: {totalOrderValue:C}");
            sb.AppendLine($"  Łączna wartość ofert   : {totalOfferValue:C}");

            return Task.FromResult(sb.ToString().TrimEnd());
        }
    }
}
