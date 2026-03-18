namespace Mastra48.Error
{
    /// <summary>
    /// Functional domain of the error.
    /// Mirrors ErrorDomain from packages/core/src/error/index.ts
    /// </summary>
    public enum ErrorDomain
    {
        TOOL,
        AGENT,
        MCP,
        AGENT_NETWORK,
        MASTRA_SERVER,
        MASTRA_OBSERVABILITY,
        MASTRA_WORKFLOW,
        MASTRA_VOICE,
        MASTRA_VECTOR,
        MASTRA_MEMORY,
        LLM,
        EVAL,
        SCORER,
        A2A,
        MASTRA_INSTANCE,
        MASTRA,
        DEPLOYER,
        STORAGE,
        MODEL_ROUTER
    }
}
