namespace Claims.Api.Domain.Claims;

public sealed class Claimant
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string FullName { get; set; }
    public string? Phone { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<Claim> Claims { get; } = [];
}
