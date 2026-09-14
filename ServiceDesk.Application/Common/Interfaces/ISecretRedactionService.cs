namespace ServiceDesk.Application.Common.Interfaces;

public interface ISecretRedactionService
{
    string RedactSecrets(string input);
    bool ContainsSecret(string input);
}
