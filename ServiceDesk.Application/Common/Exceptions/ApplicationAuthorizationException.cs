namespace ServiceDesk.Application.Common.Exceptions;

public class ApplicationAuthorizationException : Exception
{
    public ApplicationAuthorizationException(string message) : base(message)
    {
    }
}
