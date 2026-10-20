namespace Claims.Api.Domain.Claims;

public sealed class MotorClaimDetail
{
    public Guid ClaimId { get; set; }
    public required string VehicleRegistration { get; set; }
    public required Claim Claim { get; set; }
}

public sealed class PropertyClaimDetail
{
    public Guid ClaimId { get; set; }
    public required string PropertyAddress { get; set; }
    public required Claim Claim { get; set; }
}
