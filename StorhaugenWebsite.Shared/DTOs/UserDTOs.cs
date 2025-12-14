namespace StorhaugenWebsite.Shared.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public required string UniqueShareId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserDto
{
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
}

public class UpdateUserDto
{
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
}
