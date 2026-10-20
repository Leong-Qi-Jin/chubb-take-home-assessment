using Claims.Api.Domain.Claims;

namespace Claims.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Callers created in each test database. Tests create their own claims, so they do not depend on demo seed data.
/// </summary>
public static class TestUsers
{
    public static readonly Claimant Alex = new()
    {
        Id = Guid.Parse("7d1f3c2a-0001-4a5b-9c3d-2e1f0a9b8c7d"),
        Email = "alex.tan@example.com",
        FullName = "Alex Tan",
        Phone = "+65 8123 4567",
        CreatedAt = new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero),
    };

    public static readonly Claimant Maya = new()
    {
        Id = Guid.Parse("7d1f3c2a-0002-4a5b-9c3d-2e1f0a9b8c7d"),
        Email = "maya.lim@example.com",
        FullName = "Maya Lim",
        CreatedAt = new DateTimeOffset(2026, 2, 5, 0, 0, 0, TimeSpan.Zero),
    };

    // Used only by the claimant pagination test, so its claim count is known.
    public static readonly Claimant Daniel = new()
    {
        Id = Guid.Parse("7d1f3c2a-0003-4a5b-9c3d-2e1f0a9b8c7d"),
        Email = "daniel.wong@example.com",
        FullName = "Daniel Wong",
        CreatedAt = new DateTimeOffset(2026, 3, 5, 0, 0, 0, TimeSpan.Zero),
    };

    public static readonly StaffMember Sam = new()
    {
        Id = Guid.Parse("7d1f3c2a-0101-4a5b-9c3d-2e1f0a9b8c7d"),
        Email = "sam.lee@claims.example",
        DisplayName = "Sam Lee",
        IsActive = true,
    };

    public static readonly StaffMember Aisha = new()
    {
        Id = Guid.Parse("7d1f3c2a-0102-4a5b-9c3d-2e1f0a9b8c7d"),
        Email = "aisha.khan@claims.example",
        DisplayName = "Aisha Khan",
        IsActive = true,
    };

    public static readonly StaffMember FormerOfficer = new()
    {
        Id = Guid.Parse("7d1f3c2a-0104-4a5b-9c3d-2e1f0a9b8c7d"),
        Email = "former.officer@claims.example",
        DisplayName = "Former Officer",
        IsActive = false,
    };

    public static Claimant[] Claimants => [Alex, Maya, Daniel];

    public static StaffMember[] Officers => [Sam, Aisha, FormerOfficer];
}
