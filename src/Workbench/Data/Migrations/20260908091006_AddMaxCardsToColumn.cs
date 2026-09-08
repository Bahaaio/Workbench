using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workbench.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxCardsToColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoardCard_BoardColumn_ColumnId",
                table: "BoardCard");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardCard_Boards_BoardId",
                table: "BoardCard");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardCard_Issues_IssueId",
                table: "BoardCard");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardColumn_Boards_BoardId",
                table: "BoardColumn");

            migrationBuilder.DropForeignKey(
                name: "FK_MilestoneItem_Issues_IssueId",
                table: "MilestoneItem");

            migrationBuilder.DropForeignKey(
                name: "FK_MilestoneItem_Milestones_MilestoneId",
                table: "MilestoneItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MilestoneItem",
                table: "MilestoneItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BoardColumn",
                table: "BoardColumn");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BoardCard",
                table: "BoardCard");

            migrationBuilder.RenameTable(
                name: "MilestoneItem",
                newName: "MilestoneItems");

            migrationBuilder.RenameTable(
                name: "BoardColumn",
                newName: "BoardColumns");

            migrationBuilder.RenameTable(
                name: "BoardCard",
                newName: "BoardCards");

            migrationBuilder.RenameIndex(
                name: "IX_MilestoneItem_IssueId",
                table: "MilestoneItems",
                newName: "IX_MilestoneItems_IssueId");

            migrationBuilder.RenameIndex(
                name: "IX_BoardColumn_BoardId_Position",
                table: "BoardColumns",
                newName: "IX_BoardColumns_BoardId_Position");

            migrationBuilder.RenameIndex(
                name: "IX_BoardCard_IssueId",
                table: "BoardCards",
                newName: "IX_BoardCards_IssueId");

            migrationBuilder.RenameIndex(
                name: "IX_BoardCard_ColumnId_Position",
                table: "BoardCards",
                newName: "IX_BoardCards_ColumnId_Position");

            migrationBuilder.RenameIndex(
                name: "IX_BoardCard_BoardId_IssueId",
                table: "BoardCards",
                newName: "IX_BoardCards_BoardId_IssueId");

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "Projects",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Comments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "MilestoneId",
                table: "Attachments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxCards",
                table: "BoardColumns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MilestoneItems",
                table: "MilestoneItems",
                columns: new[] { "MilestoneId", "IssueId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_BoardColumns",
                table: "BoardColumns",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BoardCards",
                table: "BoardCards",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_MilestoneId",
                table: "Attachments",
                column: "MilestoneId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_Milestones_MilestoneId",
                table: "Attachments",
                column: "MilestoneId",
                principalTable: "Milestones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardCards_BoardColumns_ColumnId",
                table: "BoardCards",
                column: "ColumnId",
                principalTable: "BoardColumns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardCards_Boards_BoardId",
                table: "BoardCards",
                column: "BoardId",
                principalTable: "Boards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardCards_Issues_IssueId",
                table: "BoardCards",
                column: "IssueId",
                principalTable: "Issues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardColumns_Boards_BoardId",
                table: "BoardColumns",
                column: "BoardId",
                principalTable: "Boards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MilestoneItems_Issues_IssueId",
                table: "MilestoneItems",
                column: "IssueId",
                principalTable: "Issues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MilestoneItems_Milestones_MilestoneId",
                table: "MilestoneItems",
                column: "MilestoneId",
                principalTable: "Milestones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_Milestones_MilestoneId",
                table: "Attachments");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardCards_BoardColumns_ColumnId",
                table: "BoardCards");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardCards_Boards_BoardId",
                table: "BoardCards");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardCards_Issues_IssueId",
                table: "BoardCards");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardColumns_Boards_BoardId",
                table: "BoardColumns");

            migrationBuilder.DropForeignKey(
                name: "FK_MilestoneItems_Issues_IssueId",
                table: "MilestoneItems");

            migrationBuilder.DropForeignKey(
                name: "FK_MilestoneItems_Milestones_MilestoneId",
                table: "MilestoneItems");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_MilestoneId",
                table: "Attachments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MilestoneItems",
                table: "MilestoneItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BoardColumns",
                table: "BoardColumns");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BoardCards",
                table: "BoardCards");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "MilestoneId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "MaxCards",
                table: "BoardColumns");

            migrationBuilder.RenameTable(
                name: "MilestoneItems",
                newName: "MilestoneItem");

            migrationBuilder.RenameTable(
                name: "BoardColumns",
                newName: "BoardColumn");

            migrationBuilder.RenameTable(
                name: "BoardCards",
                newName: "BoardCard");

            migrationBuilder.RenameIndex(
                name: "IX_MilestoneItems_IssueId",
                table: "MilestoneItem",
                newName: "IX_MilestoneItem_IssueId");

            migrationBuilder.RenameIndex(
                name: "IX_BoardColumns_BoardId_Position",
                table: "BoardColumn",
                newName: "IX_BoardColumn_BoardId_Position");

            migrationBuilder.RenameIndex(
                name: "IX_BoardCards_IssueId",
                table: "BoardCard",
                newName: "IX_BoardCard_IssueId");

            migrationBuilder.RenameIndex(
                name: "IX_BoardCards_ColumnId_Position",
                table: "BoardCard",
                newName: "IX_BoardCard_ColumnId_Position");

            migrationBuilder.RenameIndex(
                name: "IX_BoardCards_BoardId_IssueId",
                table: "BoardCard",
                newName: "IX_BoardCard_BoardId_IssueId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MilestoneItem",
                table: "MilestoneItem",
                columns: new[] { "MilestoneId", "IssueId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_BoardColumn",
                table: "BoardColumn",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BoardCard",
                table: "BoardCard",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardCard_BoardColumn_ColumnId",
                table: "BoardCard",
                column: "ColumnId",
                principalTable: "BoardColumn",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardCard_Boards_BoardId",
                table: "BoardCard",
                column: "BoardId",
                principalTable: "Boards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardCard_Issues_IssueId",
                table: "BoardCard",
                column: "IssueId",
                principalTable: "Issues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardColumn_Boards_BoardId",
                table: "BoardColumn",
                column: "BoardId",
                principalTable: "Boards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MilestoneItem_Issues_IssueId",
                table: "MilestoneItem",
                column: "IssueId",
                principalTable: "Issues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MilestoneItem_Milestones_MilestoneId",
                table: "MilestoneItem",
                column: "MilestoneId",
                principalTable: "Milestones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
