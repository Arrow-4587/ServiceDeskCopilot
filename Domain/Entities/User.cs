using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    private User() { }

    public User(string username, string email, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("Username cannot be empty.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty.");

        Id = Guid.NewGuid();
        Username = username.Trim();
        Email = email.Trim().ToLowerInvariant();
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }
}
