using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Xpress_backend_V2.Migrations
{
    /// <inheritdoc />
    public partial class changedDatabaseforMultipleWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedWorkflowId",
                table: "TravelRequests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportingManagerEmail",
                table: "TravelRequests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkflowTemplates",
                columns: table => new
                {
                    WorkflowId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkflowName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WorkflowDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTemplates", x => x.WorkflowId);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowHistory",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OldWorkflowId = table.Column<int>(type: "integer", nullable: true),
                    NewWorkflowId = table.Column<int>(type: "integer", nullable: false),
                    ChangedBy = table.Column<int>(type: "integer", nullable: false),
                    ChangeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowHistory", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_WorkflowHistory_TravelRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "TravelRequests",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowHistory_Users_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowHistory_WorkflowTemplates_NewWorkflowId",
                        column: x => x.NewWorkflowId,
                        principalTable: "WorkflowTemplates",
                        principalColumn: "WorkflowId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowHistory_WorkflowTemplates_OldWorkflowId",
                        column: x => x.OldWorkflowId,
                        principalTable: "WorkflowTemplates",
                        principalColumn: "WorkflowId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowRules",
                columns: table => new
                {
                    RuleId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkflowId = table.Column<int>(type: "integer", nullable: false),
                    UserRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProjectCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowRules", x => x.RuleId);
                    table.ForeignKey(
                        name: "FK_WorkflowRules_RMTs_ProjectCode",
                        column: x => x.ProjectCode,
                        principalTable: "RMTs",
                        principalColumn: "ProjectCode",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowRules_WorkflowTemplates_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "WorkflowTemplates",
                        principalColumn: "WorkflowId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowSteps",
                columns: table => new
                {
                    StepId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkflowId = table.Column<int>(type: "integer", nullable: false),
                    StatusId = table.Column<int>(type: "integer", nullable: false),
                    StepOrder = table.Column<int>(type: "integer", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ApproverRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    StepDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSteps", x => x.StepId);
                    table.ForeignKey(
                        name: "FK_WorkflowSteps_RequestStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "RequestStatuses",
                        principalColumn: "StatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowSteps_WorkflowTemplates_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "WorkflowTemplates",
                        principalColumn: "WorkflowId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TravelRequests_AssignedWorkflowId",
                table: "TravelRequests",
                column: "AssignedWorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowHistory_ChangedBy",
                table: "WorkflowHistory",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowHistory_NewWorkflowId",
                table: "WorkflowHistory",
                column: "NewWorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowHistory_OldWorkflowId",
                table: "WorkflowHistory",
                column: "OldWorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowHistory_RequestId",
                table: "WorkflowHistory",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRules_Priority_UserRole_ProjectCode",
                table: "WorkflowRules",
                columns: new[] { "Priority", "UserRole", "ProjectCode" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRules_ProjectCode",
                table: "WorkflowRules",
                column: "ProjectCode");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRules_WorkflowId",
                table: "WorkflowRules",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSteps_StatusId",
                table: "WorkflowSteps",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSteps_WorkflowId_StepOrder",
                table: "WorkflowSteps",
                columns: new[] { "WorkflowId", "StepOrder" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TravelRequests_WorkflowTemplates_AssignedWorkflowId",
                table: "TravelRequests",
                column: "AssignedWorkflowId",
                principalTable: "WorkflowTemplates",
                principalColumn: "WorkflowId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TravelRequests_WorkflowTemplates_AssignedWorkflowId",
                table: "TravelRequests");

            migrationBuilder.DropTable(
                name: "WorkflowHistory");

            migrationBuilder.DropTable(
                name: "WorkflowRules");

            migrationBuilder.DropTable(
                name: "WorkflowSteps");

            migrationBuilder.DropTable(
                name: "WorkflowTemplates");

            migrationBuilder.DropIndex(
                name: "IX_TravelRequests_AssignedWorkflowId",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "AssignedWorkflowId",
                table: "TravelRequests");

            migrationBuilder.DropColumn(
                name: "ReportingManagerEmail",
                table: "TravelRequests");
        }
    }
}
