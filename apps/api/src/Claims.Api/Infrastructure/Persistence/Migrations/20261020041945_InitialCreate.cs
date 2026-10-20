using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Claims.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Claimants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claimants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClaimantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PolicyNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IncidentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IncidentLocation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IncidentDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AssignedOfficerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstimatedLiability = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.Id);
                    table.CheckConstraint("CK_Claims_Assignment", "([Status] = 'Submitted' AND [AssignedOfficerId] IS NULL) OR ([Status] <> 'Submitted' AND [AssignedOfficerId] IS NOT NULL)");
                    table.CheckConstraint("CK_Claims_CurrencyCode", "LEN([CurrencyCode]) = 3 AND [CurrencyCode] NOT LIKE '%[^A-Z]%'");
                    table.CheckConstraint("CK_Claims_EstimatedLiability", "[EstimatedLiability] IS NULL OR [EstimatedLiability] >= 0");
                    table.CheckConstraint("CK_Claims_IncidentAt", "[IncidentAt] <= [SubmittedAt]");
                    table.CheckConstraint("CK_Claims_SettledEstimate", "[Status] <> 'Settled' OR [EstimatedLiability] IS NOT NULL");
                    table.CheckConstraint("CK_Claims_Status", "[Status] IN ('Submitted', 'UnderReview', 'AwaitingInformation', 'Settled', 'Rejected')");
                    table.CheckConstraint("CK_Claims_Type", "[Type] IN ('Motor', 'Property')");
                    table.ForeignKey(
                        name: "FK_Claims_Claimants_ClaimantId",
                        column: x => x.ClaimantId,
                        principalTable: "Claimants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Claims_StaffMembers_AssignedOfficerId",
                        column: x => x.AssignedOfficerId,
                        principalTable: "StaffMembers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ClaimDecisions",
                columns: table => new
                {
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecidedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SettlementAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Rationale = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimDecisions", x => x.ClaimId);
                    table.CheckConstraint("CK_ClaimDecisions_Outcome", "[Outcome] IN ('Settled', 'Rejected')");
                    table.CheckConstraint("CK_ClaimDecisions_SettlementAmount", "([Outcome] = 'Settled' AND [SettlementAmount] IS NOT NULL AND [SettlementAmount] >= 0) OR ([Outcome] = 'Rejected' AND [SettlementAmount] IS NULL)");
                    table.ForeignKey(
                        name: "FK_ClaimDecisions_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClaimDecisions_StaffMembers_DecidedById",
                        column: x => x.DecidedById,
                        principalTable: "StaffMembers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InformationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ResponseText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RespondedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InformationRequests", x => x.Id);
                    table.UniqueConstraint("AK_InformationRequests_ClaimId_Id", x => new { x.ClaimId, x.Id });
                    table.CheckConstraint("CK_InformationRequests_State", "([Status] = 'Open' AND [ResponseText] IS NULL AND [RespondedAt] IS NULL AND [CancelledById] IS NULL AND [CancelledAt] IS NULL AND [CancellationReason] IS NULL) OR ([Status] = 'Answered' AND [ResponseText] IS NOT NULL AND [RespondedAt] IS NOT NULL AND [CancelledById] IS NULL AND [CancelledAt] IS NULL AND [CancellationReason] IS NULL) OR ([Status] = 'Cancelled' AND [ResponseText] IS NULL AND [RespondedAt] IS NULL AND [CancelledById] IS NOT NULL AND [CancelledAt] IS NOT NULL AND [CancellationReason] IS NOT NULL)");
                    table.CheckConstraint("CK_InformationRequests_Status", "[Status] IN ('Open', 'Answered', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_InformationRequests_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InformationRequests_StaffMembers_CancelledById",
                        column: x => x.CancelledById,
                        principalTable: "StaffMembers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InformationRequests_StaffMembers_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "StaffMembers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MotorClaimDetails",
                columns: table => new
                {
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleRegistration = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MotorClaimDetails", x => x.ClaimId);
                    table.ForeignKey(
                        name: "FK_MotorClaimDetails_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PropertyClaimDetails",
                columns: table => new
                {
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyClaimDetails", x => x.ClaimId);
                    table.ForeignKey(
                        name: "FK_PropertyClaimDetails_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ClaimHistoryEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ActorType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActorClaimantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorStaffMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InformationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Details = table.Column<string>(type: "json", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimHistoryEntries", x => x.Id);
                    table.CheckConstraint("CK_ClaimHistoryEntries_Actor", "([ActorType] = 'Claimant' AND [ActorClaimantId] IS NOT NULL AND [ActorStaffMemberId] IS NULL) OR ([ActorType] = 'ClaimsOfficer' AND [ActorClaimantId] IS NULL AND [ActorStaffMemberId] IS NOT NULL) OR ([ActorType] = 'System' AND [ActorClaimantId] IS NULL AND [ActorStaffMemberId] IS NULL)");
                    table.CheckConstraint("CK_ClaimHistoryEntries_ActorType", "[ActorType] IN ('Claimant', 'ClaimsOfficer', 'System')");
                    table.CheckConstraint("CK_ClaimHistoryEntries_EventType", "[EventType] IN ('Submitted', 'OfficerAssigned', 'EstimatedLiabilityUpdated', 'InformationRequested', 'InformationProvided', 'InformationRequestCancelled', 'Settled', 'Rejected')");
                    table.CheckConstraint("CK_ClaimHistoryEntries_InformationRequest", "([EventType] IN ('InformationRequested', 'InformationProvided', 'InformationRequestCancelled') AND [InformationRequestId] IS NOT NULL) OR ([EventType] NOT IN ('InformationRequested', 'InformationProvided', 'InformationRequestCancelled') AND [InformationRequestId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_ClaimHistoryEntries_Claimants_ActorClaimantId",
                        column: x => x.ActorClaimantId,
                        principalTable: "Claimants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClaimHistoryEntries_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClaimHistoryEntries_InformationRequests_ClaimId_InformationRequestId",
                        columns: x => new { x.ClaimId, x.InformationRequestId },
                        principalTable: "InformationRequests",
                        principalColumns: new[] { "ClaimId", "Id" });
                    table.ForeignKey(
                        name: "FK_ClaimHistoryEntries_StaffMembers_ActorStaffMemberId",
                        column: x => x.ActorStaffMemberId,
                        principalTable: "StaffMembers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Claimants_Email",
                table: "Claimants",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClaimDecisions_DecidedById",
                table: "ClaimDecisions",
                column: "DecidedById");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimHistoryEntries_ActorClaimantId",
                table: "ClaimHistoryEntries",
                column: "ActorClaimantId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimHistoryEntries_ActorStaffMemberId",
                table: "ClaimHistoryEntries",
                column: "ActorStaffMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimHistoryEntries_ClaimId_InformationRequestId",
                table: "ClaimHistoryEntries",
                columns: new[] { "ClaimId", "InformationRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimHistoryEntries_ClaimId_OccurredAt",
                table: "ClaimHistoryEntries",
                columns: new[] { "ClaimId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Claims_AssignedOfficerId_Status_UpdatedAt",
                table: "Claims",
                columns: new[] { "AssignedOfficerId", "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Claims_ClaimantId_SubmittedAt",
                table: "Claims",
                columns: new[] { "ClaimantId", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Reference",
                table: "Claims",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Claims_Status_SubmittedAt",
                table: "Claims",
                columns: new[] { "Status", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_InformationRequests_CancelledById",
                table: "InformationRequests",
                column: "CancelledById");

            migrationBuilder.CreateIndex(
                name: "IX_InformationRequests_ClaimId",
                table: "InformationRequests",
                column: "ClaimId",
                unique: true,
                filter: "[Status] = 'Open'");

            migrationBuilder.CreateIndex(
                name: "IX_InformationRequests_RequestedById",
                table: "InformationRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_StaffMembers_Email",
                table: "StaffMembers",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClaimDecisions");

            migrationBuilder.DropTable(
                name: "ClaimHistoryEntries");

            migrationBuilder.DropTable(
                name: "MotorClaimDetails");

            migrationBuilder.DropTable(
                name: "PropertyClaimDetails");

            migrationBuilder.DropTable(
                name: "InformationRequests");

            migrationBuilder.DropTable(
                name: "Claims");

            migrationBuilder.DropTable(
                name: "Claimants");

            migrationBuilder.DropTable(
                name: "StaffMembers");
        }
    }
}
