using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleJira.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class IssueConstraintsAndLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Issues",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Summary",
                table: "Issues",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "StoryPoints",
                table: "Issues",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Body",
                table: "Comments",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Users_AuthorId",
                table: "Comments");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Users_AuthorId",
                table: "Comments",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_IssueLinks_Issues_LinkedIssueId",
                table: "IssueLinks",
                column: "LinkedIssueId",
                principalTable: "Issues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.CreateIndex(
                name: "IX_IssueLinks_LinkedIssueId",
                table: "IssueLinks",
                column: "LinkedIssueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Users_AuthorId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_IssueLinks_Issues_LinkedIssueId",
                table: "IssueLinks");

            migrationBuilder.DropIndex(
                name: "IX_IssueLinks_LinkedIssueId",
                table: "IssueLinks");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Issues",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Summary",
                table: "Issues",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "StoryPoints",
                table: "Issues",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Body",
                table: "Comments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Users_AuthorId",
                table: "Comments",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
