using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSharpDriver.Migrations.PropertiesStorage
{
    public partial class PropertiesStorageContext001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CloudProperties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationName = table.Column<string>(type: "TEXT", nullable: true),
                    PropertyName = table.Column<string>(type: "TEXT", nullable: true),
                    PrefixPropertyName = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerPrimaryKey = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SerializedDataJson = table.Column<string>(type: "TEXT", nullable: true),
                    TypeName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloudProperties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CloudTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ApplicationName = table.Column<string>(type: "TEXT", nullable: true),
                    PropertyName = table.Column<string>(type: "TEXT", nullable: true),
                    PrefixPropertyName = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerPrimaryKey = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NormalizedTagNameUpper = table.Column<string>(type: "TEXT", nullable: true),
                    TagName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloudTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lockers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Token = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lockers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rubrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IsDisabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    LastUpdatedAtUTC = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUTC = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    SortIndex = table.Column<uint>(type: "INTEGER", nullable: false),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true),
                    ContextName = table.Column<string>(type: "TEXT", nullable: true),
                    NormalizedNameUpper = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rubrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rubrics_Rubrics_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Rubrics",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CloudProperties_ApplicationName_PropertyName",
                table: "CloudProperties",
                columns: new[] { "ApplicationName", "PropertyName" });

            migrationBuilder.CreateIndex(
                name: "IX_CloudProperties_CreatedAt",
                table: "CloudProperties",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CloudProperties_PrefixPropertyName_OwnerPrimaryKey",
                table: "CloudProperties",
                columns: new[] { "PrefixPropertyName", "OwnerPrimaryKey" });

            migrationBuilder.CreateIndex(
                name: "IX_CloudProperties_TypeName",
                table: "CloudProperties",
                column: "TypeName");

            migrationBuilder.CreateIndex(
                name: "IX_CloudTags_ApplicationName_PropertyName",
                table: "CloudTags",
                columns: new[] { "ApplicationName", "PropertyName" });

            migrationBuilder.CreateIndex(
                name: "IX_CloudTags_CreatedAt",
                table: "CloudTags",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CloudTags_NormalizedTagNameUpper",
                table: "CloudTags",
                column: "NormalizedTagNameUpper");

            migrationBuilder.CreateIndex(
                name: "IX_CloudTags_PrefixPropertyName_OwnerPrimaryKey",
                table: "CloudTags",
                columns: new[] { "PrefixPropertyName", "OwnerPrimaryKey" });

            migrationBuilder.CreateIndex(
                name: "IX_TagNameOwnerKeyUnique",
                table: "CloudTags",
                columns: new[] { "NormalizedTagNameUpper", "OwnerPrimaryKey", "ApplicationName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lockers_Token",
                table: "Lockers",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rubrics_ContextName",
                table: "Rubrics",
                column: "ContextName");

            migrationBuilder.CreateIndex(
                name: "IX_Rubrics_IsDisabled",
                table: "Rubrics",
                column: "IsDisabled");

            migrationBuilder.CreateIndex(
                name: "IX_Rubrics_Name",
                table: "Rubrics",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Rubrics_NormalizedNameUpper",
                table: "Rubrics",
                column: "NormalizedNameUpper");

            migrationBuilder.CreateIndex(
                name: "IX_Rubrics_NormalizedNameUpper_ContextName",
                table: "Rubrics",
                columns: new[] { "NormalizedNameUpper", "ContextName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rubrics_ParentId",
                table: "Rubrics",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Rubrics_SortIndex_ParentId_ContextName",
                table: "Rubrics",
                columns: new[] { "SortIndex", "ParentId", "ContextName" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CloudProperties");

            migrationBuilder.DropTable(
                name: "CloudTags");

            migrationBuilder.DropTable(
                name: "Lockers");

            migrationBuilder.DropTable(
                name: "Rubrics");
        }
    }
}
