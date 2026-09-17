using System.Text.Json;
using System.Text.RegularExpressions;

namespace ServiceDesk.Application.Prompts;

public static class PromptOutputValidator
{
    private static readonly Regex PromptInjectionRegex = new(
        @"(?i)(ignore\s+previous\s+instructions|system\s+prompt|disregard\s+rules|reveal\s+secret|bypass\s+auth|dump\s+database)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool DetectPromptInjection(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return PromptInjectionRegex.IsMatch(input);
    }

    public static bool ValidateStructuredJsonOutput<T>(string jsonOutput, out T? parsedObject)
    {
        parsedObject = default;
        if (string.IsNullOrWhiteSpace(jsonOutput))
            return false;

        try
        {
            parsedObject = JsonSerializer.Deserialize<T>(jsonOutput);
            return parsedObject != null;
        }
        catch
        {
            return false;
        }
    }
}
