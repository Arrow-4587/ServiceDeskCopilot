using System.Text.RegularExpressions;
using ServiceDesk.Application.Common.Interfaces;

namespace ServiceDesk.Infrastructure.Services;

public class SecretRedactionService : ISecretRedactionService
{
    private static readonly Regex SecretRegexPattern = new(
        @"(?i)(api[-_]?key|secret|password|bearer\s+[a-z0-9\-_\.=]+|AccountKey=[a-z0-9+/=]+|DefaultEndpointsProtocol=[^;]+|Server=[^;]+;Database=[^;]+|sk-[a-zA-Z0-9]{20,}|-----BEGIN\s+PRIVATE\s+KEY-----)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string RedactSecrets(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return SecretRegexPattern.Replace(input, "[REDACTED_SECRET]");
    }

    public bool ContainsSecret(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return SecretRegexPattern.IsMatch(input);
    }
}
