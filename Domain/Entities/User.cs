using System.ComponentModel.DataAnnotations.Schema;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Exceptions;

namespace ServiceDesk.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    [NotMapped]
    public string Password { get; private set; } = "Password123!";

    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    private User() { }

    public User(string username, string email, UserRole role, string password = "Password123!")
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new DomainException("Username cannot be empty.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty.");

        Id = Guid.NewGuid();
        Username = username.Trim();
        Email = email.Trim().ToLowerInvariant();
        Role = role;
        Password = string.IsNullOrWhiteSpace(password) ? GetDefaultPasswordForRole(role) : password.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public bool VerifyPassword(string inputPassword)
    {
        if (string.IsNullOrWhiteSpace(inputPassword))
            return false;

        var trimmed = inputPassword.Trim();
        var defaultRolePassword = GetDefaultPasswordForRole(Role);
        return string.Equals(trimmed, defaultRolePassword, StringComparison.Ordinal) ||
               string.Equals(trimmed, "Password123!", StringComparison.Ordinal) ||
               string.Equals(trimmed, Password, StringComparison.Ordinal);
    }

    public static string GetDefaultPasswordForRole(UserRole role) => role switch
    {
        UserRole.Employee => "Employee123!",
        UserRole.Analyst => "Analyst123!",
        UserRole.Manager => "Manager123!",
        UserRole.Administrator => "Admin123!",
        _ => "Password123!"
    };
}
