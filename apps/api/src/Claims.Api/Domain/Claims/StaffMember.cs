namespace Claims.Api.Domain.Claims;

public sealed class StaffMember
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public bool IsActive { get; set; }
    public ICollection<Claim> AssignedClaims { get; } = [];
}
