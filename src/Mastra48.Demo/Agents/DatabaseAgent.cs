using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Mastra48.Demo.Services;

namespace Mastra48.Demo.Agents
{
    /// <summary>
    /// DatabaseAgent – translates natural language to SQL (shown for transparency),
    /// executes it against the in-memory data store, and returns formatted results.
    ///
    /// Can collaborate with FileAgent to save query results to a file.
    ///
    /// Trace output is written in grey.
    /// </summary>
    public class DatabaseAgent : AgentBase
    {
        private readonly DatabaseService _db;
        private readonly FileAgent _fileAgent; // optional collaborator

        public DatabaseAgent(DatabaseService db, FileAgent fileAgent = null)
            : base("DatabaseAgent")
        {
            _db = db ?? throw new ArgumentNullException("db");
            _fileAgent = fileAgent;
        }

        // ----------------------------------------------------------------
        // Main query entry point
        // ----------------------------------------------------------------

        /// <summary>
        /// Parses a natural-language <paramref name="query"/>, shows the generated SQL,
        /// executes it against in-memory data, and returns a formatted result.
        /// </summary>
        public Task<string> QueryAsync(string query)
        {
            Log($"Analizuję zapytanie: \"{query}\"");
            LogStep("Tłumaczenie języka naturalnego → SQL...");

            string sql;
            string results;

            try
            {
                (sql, results) = _db.ExecuteNaturalQuery(query);
            }
            catch (Exception ex)
            {
                LogError($"Błąd wykonania zapytania: {ex.Message}");
                return Task.FromResult<string>($"Błąd: {ex.Message}");
            }

            LogStep($"Wygenerowane SQL: {sql}");
            LogStep("Wykonuję zapytanie na danych in-memory...");
            Log("Zapytanie zakończone");

            var sb = new StringBuilder();
            sb.AppendLine($"📊 Zapytanie do bazy danych");
            sb.AppendLine($"SQL: {sql}");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine(results);

            return Task.FromResult(sb.ToString().TrimEnd());
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
