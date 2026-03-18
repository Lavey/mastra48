namespace Mastra48.Error
{
    /// <summary>
    /// Broad category of the error.
    /// Mirrors ErrorCategory from packages/core/src/error/index.ts
    /// </summary>
    public enum ErrorCategory
    {
        UNKNOWN,
        USER,
        SYSTEM,
        THIRD_PARTY
    }
}
