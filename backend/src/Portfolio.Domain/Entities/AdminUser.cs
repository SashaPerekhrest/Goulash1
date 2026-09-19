namespace Portfolio.Domain.Entities;

public sealed class AdminUser
{
    public Guid Id { get; set; }

    public string Login { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
