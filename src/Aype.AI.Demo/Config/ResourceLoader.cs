using System;
using System.IO;
using System.Reflection;

namespace Aype.AI.Demo.Config
{
    /// <summary>
    /// Utility class for loading embedded Markdown resource files.
    ///
    /// Resources in the Agents/Prompts/ folder are included with
    /// Build Action = EmbeddedResource and accessed via the assembly manifest.
    ///
    /// Resource naming convention:
    ///   Aype.AI.Demo.Agents.Prompts.{FileName}.md
    /// </summary>
    public static class ResourceLoader
    {
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        private const string ResourcePrefix = "Aype.AI.Demo.Agents.Prompts.";

        /// <summary>
        /// Loads the content of an embedded Markdown resource file.
        /// </summary>
        /// <param name="fileName">
        /// The file name, e.g. "ChatAgent.md" or "NaturalLanguageToSqlAgent.md".
        /// </param>
        /// <returns>
        /// The full text content of the resource, or an empty string when the
        /// resource cannot be found.
        /// </returns>
        public static string Load(string fileName)
        {
            var resourceName = ResourcePrefix + fileName;
            using (var stream = _assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Console.Error.WriteLine($"[ResourceLoader] Zasób nie znaleziony: {resourceName}");
                    return string.Empty;
                }

                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }
    }
}
