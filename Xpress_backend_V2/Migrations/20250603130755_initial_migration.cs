using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Xpress_backend_V2.Migrations
{
    /// <inheritdoc />
    public partial class initial_migration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Airlines",
                columns: table => new
                {
                    AirlineId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AirlineName = table.Column<string>(type: "text", nullable: false),
                    AirlineExpense = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Airlines", x => x.AirlineId);
                });

            migrationBuilder.CreateTable(
                name: "RequestStatuses",
                columns: table => new
                {
                    StatusId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StatusName = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestStatuses", x => x.StatusId);
                });

            migrationBuilder.CreateTable(
                name: "RMTs",
                columns: table => new
                {
                    ProjectId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectCode = table.Column<string>(type: "text", nullable: false),
                    ProjectName = table.Column<string>(type: "text", nullable: false),
                    DuId = table.Column<int>(type: "integer", nullable: false),
                    ProjectStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProjectEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProjectManager = table.Column<string>(type: "text", nullable: false),
                    ProjectManagerEmail = table.Column<string>(type: "text", nullable: false),
                    ProjectStatus = table.Column<string>(type: "text", nullable: false),
                    DuHeadName = table.Column<string>(type: "text", nullable: false),
                    DuHeadEmail = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RMTs", x => x.ProjectId);
                    table.UniqueConstraint("AK_RMTs_ProjectCode", x => x.ProjectCode);
                });

            migrationBuilder.CreateTable(
                name: "TravelModes",
                columns: table => new
                {
                    TravelModeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TravelModeName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelModes", x => x.TravelModeId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeName = table.Column<string>(type: "text", nullable: false),
                    UserRole = table.Column<string>(type: "text", nullable: false),
                    Department = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "AadharDocs",
                columns: table => new
                {
                    AadharId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AadharName = table.Column<string>(type: "text", nullable: false),
                    DocumentNumber = table.Column<string>(type: "text", nullable: true),
                    DocumentPath = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AadharDocs", x => x.AadharId);
                    table.ForeignKey(
                        name: "FK_AadharDocs_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AadharDocs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NotificationTitle = table.Column<string>(type: "text", nullable: false),
                    NotificationDescription = table.Column<string>(type: "text", nullable: false),
                    NotificationTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PassportDocs",
                columns: table => new
                {
                    PassportDocId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PassportNumber = table.Column<string>(type: "text", nullable: false),
                    IssuingCountry = table.Column<string>(type: "text", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DocumentPath = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PassportDocs", x => x.PassportDocId);
                    table.ForeignKey(
                        name: "FK_PassportDocs_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PassportDocs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VisaDocs",
                columns: table => new
                {
                    VisaDocId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    VisaNumber = table.Column<string>(type: "text", nullable: false),
                    VisaType = table.Column<string>(type: "text", nullable: false),
                    IssuingCountry = table.Column<string>(type: "text", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VisaClass = table.Column<string>(type: "text", nullable: false),
                    DocumentPath = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisaDocs", x => x.VisaDocId);
                    table.ForeignKey(
                        name: "FK_VisaDocs_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VisaDocs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    UserNotificationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    NotificationId = table.Column<int>(type: "integer", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.UserNotificationId);
                    table.ForeignKey(
                        name: "FK_UserNotifications_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "NotificationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserNotifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OldStatusId = table.Column<int>(type: "integer", nullable: true),
                    NewStatusId = table.Column<int>(type: "integer", nullable: true),
                    ChangeDescription = table.Column<string>(type: "text", nullable: true),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_AuditLogs_RequestStatuses_NewStatusId",
                        column: x => x.NewStatusId,
                        principalTable: "RequestStatuses",
                        principalColumn: "StatusId");
                    table.ForeignKey(
                        name: "FK_AuditLogs_RequestStatuses_OldStatusId",
                        column: x => x.OldStatusId,
                        principalTable: "RequestStatuses",
                        principalColumn: "StatusId");
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketOptions",
                columns: table => new
                {
                    OptionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    OptionDescription = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSelected = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketOptions", x => x.OptionId);
                    table.ForeignKey(
                        name: "FK_TicketOptions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TravelRequests",
                columns: table => new
                {
                    RequestId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TravelModeId = table.Column<int>(type: "integer", nullable: false),
                    IsInternational = table.Column<bool>(type: "boolean", nullable: false),
                    IsRoundTrip = table.Column<bool>(type: "boolean", nullable: false),
                    ProjectCode = table.Column<string>(type: "text", nullable: false),
                    SourcePlace = table.Column<string>(type: "text", nullable: false),
                    SourceCountry = table.Column<string>(type: "text", nullable: false),
                    DestinationPlace = table.Column<string>(type: "text", nullable: false),
                    DestinationCountry = table.Column<string>(type: "text", nullable: false),
                    OutboundDepartureDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OutboundArrivalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReturnDepartureDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReturnArrivalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsAccommodationRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsDropOffRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsPickUpRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Comments = table.Column<string>(type: "text", nullable: true),
                    PurposeOfTravel = table.Column<string>(type: "text", nullable: false),
                    IsVegetarian = table.Column<bool>(type: "boolean", nullable: false),
                    FoodComment = table.Column<string>(type: "text", nullable: true),
                    AttendedCCT = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentStatusId = table.Column<int>(type: "integer", nullable: false),
                    SelectedTicketOptionId = table.Column<int>(type: "integer", nullable: true),
                    TravelAgencyName = table.Column<string>(type: "text", nullable: true),
                    TravelAgencyExpense = table.Column<decimal>(type: "numeric", nullable: true),
                    AirlineId = table.Column<int>(type: "integer", nullable: true),
                    TotalExpense = table.Column<decimal>(type: "numeric", nullable: true),
                    TicketDocumentPath = table.Column<string>(type: "text", nullable: true),
                    LDCertificatePath = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelRequests", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_TravelRequests_Airlines_AirlineId",
                        column: x => x.AirlineId,
                        principalTable: "Airlines",
                        principalColumn: "AirlineId");
                    table.ForeignKey(
                        name: "FK_TravelRequests_RMTs_ProjectCode",
                        column: x => x.ProjectCode,
                        principalTable: "RMTs",
                        principalColumn: "ProjectCode",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TravelRequests_RequestStatuses_CurrentStatusId",
                        column: x => x.CurrentStatusId,
                        principalTable: "RequestStatuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TravelRequests_TicketOptions_SelectedTicketOptionId",
                        column: x => x.SelectedTicketOptionId,
                        principalTable: "TicketOptions",
                        principalColumn: "OptionId");
                    table.ForeignKey(
                        name: "FK_TravelRequests_TravelModes_TravelModeId",
                        column: x => x.TravelModeId,
                        principalTable: "TravelModes",
                        principalColumn: "TravelModeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TravelRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AadharDocs_CreatedBy",
                table: "AadharDocs",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AadharDocs_UserId",
                table: "AadharDocs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_NewStatusId",
                table: "AuditLogs",
                column: "NewStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OldStatusId",
                table: "AuditLogs",
                column: "OldStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_RequestId",
                table: "AuditLogs",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedBy",
                table: "Notifications",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PassportDocs_CreatedBy",
                table: "PassportDocs",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PassportDocs_UserId",
                table: "PassportDocs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RMTs_ProjectCode",
                table: "RMTs",
                column: "ProjectCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketOptions_CreatedByUserId",
                table: "TicketOptions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketOptions_RequestId",
                table: "TicketOptions",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_AirlineId",
                table: "TravelRequests",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_CurrentStatusId",
                table: "TravelRequests",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_ProjectCode",
                table: "TravelRequests",
                column: "ProjectCode");

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_SelectedTicketOptionId",
                table: "TravelRequests",
                column: "SelectedTicketOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_TravelModeId",
                table: "TravelRequests",
                column: "TravelModeId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_UserId",
                table: "TravelRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_NotificationId",
                table: "UserNotifications",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId",
                table: "UserNotifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VisaDocs_CreatedBy",
                table: "VisaDocs",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_VisaDocs_UserId",
                table: "VisaDocs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_TravelRequests_RequestId",
                table: "AuditLogs",
                column: "RequestId",
                principalTable: "TravelRequests",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketOptions_TravelRequests_RequestId",
                table: "TicketOptions",
                column: "RequestId",
                principalTable: "TravelRequests",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketOptions_Users_CreatedByUserId",
                table: "TicketOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_TravelRequests_Users_UserId",
                table: "TravelRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TravelRequests_RequestStatuses_CurrentStatusId",
                table: "TravelRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TicketOptions_TravelRequests_RequestId",
                table: "TicketOptions");

            migrationBuilder.DropTable(
                name: "AadharDocs");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "PassportDocs");

            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropTable(
                name: "VisaDocs");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "RequestStatuses");

            migrationBuilder.DropTable(
                name: "TravelRequests");

            migrationBuilder.DropTable(
                name: "Airlines");

            migrationBuilder.DropTable(
                name: "RMTs");

            migrationBuilder.DropTable(
                name: "TicketOptions");

            migrationBuilder.DropTable(
                name: "TravelModes");
        }
    }
}
