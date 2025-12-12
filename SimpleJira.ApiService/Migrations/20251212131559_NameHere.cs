using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleJira.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class NameHere : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReporterId",
                table: "Issues",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoryPoints",
                table: "Issues",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Issues",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IssueLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedIssueId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueLinks_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ReporterId",
                table: "Issues",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_AuthorId",
                table: "Comments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_IssueId",
                table: "Comments",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueLinks_IssueId_LinkedIssueId",
                table: "IssueLinks",
                columns: new[] { "IssueId", "LinkedIssueId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Users_ReporterId",
                table: "Issues",
                column: "ReporterId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Users_ReporterId",
                table: "Issues");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "IssueLinks");

            migrationBuilder.DropIndex(
                name: "IX_Issues_ReporterId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "ReporterId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "StoryPoints",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Issues");
        }
    }
}
