using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Aype.AI.Demo.Services
{
    /// <summary>
    /// Provides file-system operations for the FileAgent.
    /// Sensitive operations (delete, overwrite) require explicit confirmation.
    /// </summary>
    public class FileService
    {
        private readonly string _baseDirectory;

        public FileService(string baseDirectory = "demo_files")
        {
            _baseDirectory = baseDirectory;
            // Ensure the demo directory exists
            if (!Directory.Exists(_baseDirectory))
                Directory.CreateDirectory(_baseDirectory);
        }

        // ----------------------------------------------------------------
        // Search
        // ----------------------------------------------------------------

        /// <summary>
        /// Searches for files whose name contains the given pattern (case-insensitive).
        /// Searches in the base directory and its subdirectories.
        /// </summary>
        public List<string> SearchFiles(string pattern, string searchPath = null)
        {
            var root = searchPath ?? _baseDirectory;
            if (!Directory.Exists(root))
                return new List<string>();

            var results = new List<string>();
            try
            {
                foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    var name = Path.GetFileName(file);
                    if (name.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                        results.Add(file);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we cannot access
            }
            return results;
        }

        // ----------------------------------------------------------------
        // Read
        // ----------------------------------------------------------------

        /// <summary>Reads the entire content of a text file.</summary>
        public string ReadFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Plik nie istnieje: {path}");
            return File.ReadAllText(path, Encoding.UTF8);
        }

        // ----------------------------------------------------------------
        // Summarise
        // ----------------------------------------------------------------

        /// <summary>
        /// Returns a simple summary of a text file: first N lines + line/word count.
        /// When a real LLM is available the ChatAgent can enhance this with AI.
        /// </summary>
        public string SummarizeFile(string path, int previewLines = 10)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Plik nie istnieje: {path}");

            var lines = File.ReadAllLines(path, Encoding.UTF8);
            var wordCount = 0;
            foreach (var line in lines)
                wordCount += line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

            var sb = new StringBuilder();
            sb.AppendLine($"Plik      : {path}");
            sb.AppendLine($"Rozmiar   : {new FileInfo(path).Length} bajtów");
            sb.AppendLine($"Wiersze   : {lines.Length}");
            sb.AppendLine($"Słowa     : {wordCount}");
            sb.AppendLine($"Podgląd (pierwsze {Math.Min(previewLines, lines.Length)} linii):");
            sb.AppendLine(new string('-', 60));
            for (int i = 0; i < Math.Min(previewLines, lines.Length); i++)
                sb.AppendLine(lines[i]);
            if (lines.Length > previewLines)
                sb.AppendLine($"... ({lines.Length - previewLines} więcej linii)");

            return sb.ToString();
        }

        // ----------------------------------------------------------------
        // Write
        // ----------------------------------------------------------------

        /// <summary>
        /// Writes content to a file. Returns true if written successfully.
        /// Caller is responsible for requesting confirmation when overwriting.
        /// </summary>
        public void WriteFile(string path, string content)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, content, Encoding.UTF8);
        }

        /// <summary>Returns true if the file already exists (so caller can prompt).</summary>
        public bool FileExists(string path) => File.Exists(path);

        // ----------------------------------------------------------------
        // Delete
        // ----------------------------------------------------------------

        /// <summary>
        /// Deletes a file. Caller must have already obtained user confirmation.
        /// </summary>
        public void DeleteFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Plik nie istnieje: {path}");
            File.Delete(path);
        }

        // ----------------------------------------------------------------
        // List files in demo directory
        // ----------------------------------------------------------------

        /// <summary>Lists all files in the demo base directory.</summary>
        public List<string> ListFiles()
        {
            if (!Directory.Exists(_baseDirectory))
                return new List<string>();
            return new List<string>(Directory.GetFiles(_baseDirectory, "*", SearchOption.AllDirectories));
        }

        /// <summary>Returns the configured base directory path.</summary>
        public string BaseDirectory => _baseDirectory;
    }
}
