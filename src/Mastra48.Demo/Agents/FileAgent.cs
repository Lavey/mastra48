using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Mastra48.Demo.Services;

namespace Mastra48.Demo.Agents
{
    /// <summary>
    /// FileAgent – handles all file-system operations.
    ///
    /// Supported operations:
    ///   • Search files matching a pattern
    ///   • Read file content
    ///   • Summarise file content (optionally enhanced by Mistral AI)
    ///   • Write / append to a file (prompts user for confirmation when overwriting)
    ///   • Delete a file (always requires user confirmation)
    ///
    /// Writes agent-trace output in grey; sensitive operations prompt in yellow.
    /// </summary>
    public class FileAgent : AgentBase
    {
        private readonly FileService _fileService;
        private readonly MistralService _mistral; // may be null

        public FileAgent(FileService fileService, MistralService mistral = null)
            : base("FileAgent")
        {
            _fileService = fileService ?? throw new ArgumentNullException("fileService");
            _mistral = mistral;
        }

        // ----------------------------------------------------------------
        // Search
        // ----------------------------------------------------------------

        /// <summary>
        /// Searches for files whose name contains <paramref name="pattern"/>.
        /// Returns a formatted result string.
        /// </summary>
        public Task<string> SearchFilesAsync(string pattern, string searchPath = null)
        {
            Log($"Szukam plików pasujących do wzorca: \"{pattern}\"");
            var files = _fileService.SearchFiles(pattern, searchPath);
            LogStep($"Przeszukiwano: {searchPath ?? _fileService.BaseDirectory}");
            LogStep($"Znaleziono: {files.Count} plików");

            if (files.Count == 0)
                return Task.FromResult<string>($"Nie znaleziono plików pasujących do wzorca \"{pattern}\".");

            var sb = new StringBuilder();
            sb.AppendLine($"Znaleziono {files.Count} plik(ów) pasujących do \"{pattern}\":");
            foreach (var f in files)
                sb.AppendLine($"  📄 {f}");
            return Task.FromResult(sb.ToString().TrimEnd());
        }

        // ----------------------------------------------------------------
        // Read
        // ----------------------------------------------------------------

        /// <summary>
        /// Reads and returns the full content of the specified file.
        /// </summary>
        public Task<string> ReadFileAsync(string path)
        {
            Log($"Odczytuję plik: {path}");
            try
            {
                var content = _fileService.ReadFile(path);
                LogStep($"Odczytano {content.Length} znaków");
                return Task.FromResult<string>($"Zawartość pliku \"{path}\":\n{new string('-', 60)}\n{content}");
            }
            catch (FileNotFoundException ex)
            {
                LogError(ex.Message);
                return Task.FromResult<string>($"Błąd: {ex.Message}");
            }
        }

        // ----------------------------------------------------------------
        // Summarise
        // ----------------------------------------------------------------

        /// <summary>
        /// Summarises a file.  When Mistral AI is available, generates an AI summary;
        /// otherwise produces a structural summary (line/word count + preview).
        /// </summary>
        public async Task<string> SummarizeFileAsync(string path)
        {
            Log($"Sumarizuję plik: {path}");
            try
            {
                var structural = _fileService.SummarizeFile(path, previewLines: 15);
                LogStep("Obliczono strukturę pliku");

                if (_mistral != null)
                {
                    LogStep("Wysyłam zawartość do Mistral AI w celu podsumowania...");
                    try
                    {
                        var raw = _fileService.ReadFile(path);
                        var aiSummary = await _mistral.SummarizeAsync(raw);
                        return structural + "\n📝 Podsumowanie AI:\n" + aiSummary;
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"Mistral AI niedostępny – {ex.Message}. Używam podsumowania strukturalnego.");
                    }
                }

                return structural;
            }
            catch (FileNotFoundException ex)
            {
                LogError(ex.Message);
                return $"Błąd: {ex.Message}";
            }
        }

        // ----------------------------------------------------------------
        // Write (with optional overwrite confirmation)
        // ----------------------------------------------------------------

        /// <summary>
        /// Writes <paramref name="content"/> to <paramref name="path"/>.
        /// When the file already exists, asks the user for confirmation before overwriting.
        /// Returns a status message.
        /// </summary>
        public Task<string> WriteFileAsync(string path, string content, bool requireConfirmation = true)
        {
            Log($"Zapisuję plik: {path} ({content.Length} znaków)");

            if (requireConfirmation && _fileService.FileExists(path))
            {
                LogWarning($"Plik \"{path}\" już istnieje!");
                if (!ConfirmAction($"Czy na pewno chcesz nadpisać plik \"{path}\"?"))
                {
                    Log("Operacja anulowana przez użytkownika.");
                    return Task.FromResult<string>($"Operacja anulowana – plik \"{path}\" nie został zmieniony.");
                }
            }

            try
            {
                _fileService.WriteFile(path, content);
                LogStep($"Plik zapisany pomyślnie: {path}");
                return Task.FromResult<string>($"✓ Plik \"{path}\" zapisany pomyślnie ({content.Length} znaków).");
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                return Task.FromResult<string>($"Błąd zapisu pliku: {ex.Message}");
            }
        }

        // ----------------------------------------------------------------
        // Delete (always requires confirmation)
        // ----------------------------------------------------------------

        /// <summary>
        /// Deletes the specified file after user confirmation.
        /// </summary>
        public Task<string> DeleteFileAsync(string path)
        {
            Log($"Żądanie usunięcia pliku: {path}");
            LogWarning("Operacja usuwania jest nieodwracalna!");

            if (!ConfirmAction($"Czy na pewno chcesz USUNĄĆ plik \"{path}\"?"))
            {
                Log("Operacja anulowana przez użytkownika.");
                return Task.FromResult<string>($"Operacja anulowana – plik \"{path}\" nie został usunięty.");
            }

            try
            {
                _fileService.DeleteFile(path);
                LogStep($"Plik usunięty: {path}");
                return Task.FromResult<string>($"✓ Plik \"{path}\" został usunięty.");
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                return Task.FromResult<string>($"Błąd usuwania pliku: {ex.Message}");
            }
        }

        // ----------------------------------------------------------------
        // List all demo files
        // ----------------------------------------------------------------

        /// <summary>Returns a formatted list of all files in the demo directory.</summary>
        public Task<string> ListFilesAsync()
        {
            Log($"Listowanie plików w katalogu: {_fileService.BaseDirectory}");
            var files = _fileService.ListFiles();
            LogStep($"Znaleziono {files.Count} plików");

            if (files.Count == 0)
                return Task.FromResult<string>($"Katalog \"{_fileService.BaseDirectory}\" jest pusty lub nie istnieje.");

            var sb = new StringBuilder();
            sb.AppendLine($"Pliki w \"{_fileService.BaseDirectory}\" ({files.Count}):");
            foreach (var f in files)
                sb.AppendLine($"  📄 {f}");
            return Task.FromResult(sb.ToString().TrimEnd());
        }
    }
}
