using System;

namespace Mastra48.Demo.Agents
{
    /// <summary>
    /// Base class for all demo agents.
    /// Provides colour-coded console logging utilities:
    ///   - WriteInfo()  → grey  (internal agent processing)
    ///   - WriteAssistant() → green (final answer presented to user)
    ///   - WriteError() → red   (errors)
    ///   - WriteWarning() → yellow (warnings / confirmations)
    /// </summary>
    public abstract class AgentBase
    {
        protected readonly string AgentName;

        protected AgentBase(string agentName)
        {
            AgentName = agentName;
        }

        // ----------------------------------------------------------------
        // Colour-coded console helpers
        // ----------------------------------------------------------------

        /// <summary>Writes an internal agent status line in dark grey.</summary>
        protected void Log(string message)
        {
            var saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  [{AgentName}] {message}");
            Console.ForegroundColor = saved;
        }

        /// <summary>Writes a sub-step trace in grey.</summary>
        protected void LogStep(string message)
        {
            var saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"    > {message}");
            Console.ForegroundColor = saved;
        }

        /// <summary>Writes a warning or confirmation prompt in yellow.</summary>
        protected void LogWarning(string message)
        {
            var saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ⚠ [{AgentName}] {message}");
            Console.ForegroundColor = saved;
        }

        /// <summary>Writes an error message in red.</summary>
        protected void LogError(string message)
        {
            var saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ [{AgentName}] BŁĄD: {message}");
            Console.ForegroundColor = saved;
        }

        // ----------------------------------------------------------------
        // Static helpers (used by ChatAgent to print the final answer)
        // ----------------------------------------------------------------

        /// <summary>Prints the assistant's final response in green.</summary>
        public static void PrintAssistantResponse(string message)
        {
            var saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine("Agent:");
            Console.WriteLine(message);
            Console.ForegroundColor = saved;
            Console.WriteLine();
        }

        /// <summary>Prints the user prompt label in white (called before ReadLine).</summary>
        public static void PrintUserPrompt()
        {
            var saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Użytkownik: ");
            Console.ForegroundColor = saved;
        }

        /// <summary>Prompts the user for a yes/no confirmation and returns true for 'y'/'t'.</summary>
        public static bool ConfirmAction(string prompt)
        {
            var saved = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"  ⚠ POTWIERDZENIE: {prompt} [t/n]: ");
            Console.ForegroundColor = saved;
            var answer = Console.ReadLine() ?? string.Empty;
            return answer.Trim().ToLower() == "t" || answer.Trim().ToLower() == "y" || answer.Trim().ToLower() == "tak";
        }
    }
}
