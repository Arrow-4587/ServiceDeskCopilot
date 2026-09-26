namespace ServiceDesk.Infrastructure.Adapters.Ai;

public class FoundryAgentRunFailedException : Exception
{
    public FoundryAgentRunFailedException(string message) : base(message) { }
    public FoundryAgentRunFailedException(string message, Exception innerException) : base(message, innerException) { }
}

public class FoundryRateLimitException : Exception
{
    public FoundryRateLimitException(string message) : base(message) { }
    public FoundryRateLimitException(string message, Exception innerException) : base(message, innerException) { }
}

public class FoundryAgentNotFoundException : Exception
{
    public FoundryAgentNotFoundException(string message) : base(message) { }
    public FoundryAgentNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}
