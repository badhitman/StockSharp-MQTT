using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockSharpDriver.Migrations
{
    public partial class AppTelegramBotContext001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Chats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NormalizedTitleUpper = table.Column<string>(type: "TEXT", nullable: true),
                    NormalizedUsernameUpper = table.Column<string>(type: "TEXT", nullable: true),
                    NormalizedFirstNameUpper = table.Column<string>(type: "TEXT", nullable: true),
                    NormalizedLastNameUpper = table.Column<string>(type: "TEXT", nullable: true),
                    LastMessageId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChatTelegramId = table.Column<long>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    FirstName = table.Column<string>(type: "TEXT", nullable: true),
                    LastName = table.Column<string>(type: "TEXT", nullable: true),
                    IsForum = table.Column<bool>(type: "INTEGER", nullable: true),
                    LastUpdateUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserTelegramId = table.Column<long>(type: "INTEGER", nullable: false),
                    IsBot = table.Column<bool>(type: "INTEGER", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", nullable: true),
                    LastName = table.Column<string>(type: "TEXT", nullable: true),
                    Username = table.Column<string>(type: "TEXT", nullable: true),
                    LanguageCode = table.Column<string>(type: "TEXT", nullable: true),
                    IsPremium = table.Column<bool>(type: "INTEGER", nullable: true),
                    AddedToAttachmentMenu = table.Column<bool>(type: "INTEGER", nullable: true),
                    NormalizedFirstNameUpper = table.Column<string>(type: "TEXT", nullable: true),
                    NormalizedLastNameUpper = table.Column<string>(type: "TEXT", nullable: true),
                    NormalizedUsernameUpper = table.Column<string>(type: "TEXT", nullable: true),
                    LastUpdateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastMessageId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JoinsUsersToChats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChatId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JoinsUsersToChats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JoinsUsersToChats_Chats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JoinsUsersToChats_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TypeMessage = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageTelegramId = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageThreadId = table.Column<int>(type: "INTEGER", nullable: true),
                    FromId = table.Column<int>(type: "INTEGER", nullable: true),
                    ChatId = table.Column<int>(type: "INTEGER", nullable: false),
                    SenderChatId = table.Column<int>(type: "INTEGER", nullable: true),
                    ForwardFromId = table.Column<long>(type: "INTEGER", nullable: true),
                    IsTopicMessage = table.Column<bool>(type: "INTEGER", nullable: true),
                    ForwardFromChatId = table.Column<long>(type: "INTEGER", nullable: true),
                    ForwardFromMessageId = table.Column<int>(type: "INTEGER", nullable: true),
                    ForwardSignature = table.Column<string>(type: "TEXT", nullable: true),
                    ForwardSenderName = table.Column<string>(type: "TEXT", nullable: true),
                    ForwardDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsAutomaticForward = table.Column<bool>(type: "INTEGER", nullable: true),
                    ReplyToMessageId = table.Column<int>(type: "INTEGER", nullable: true),
                    ViaBotId = table.Column<long>(type: "INTEGER", nullable: true),
                    EditDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MediaGroupId = table.Column<string>(type: "TEXT", nullable: true),
                    AuthorSignature = table.Column<string>(type: "TEXT", nullable: true),
                    Text = table.Column<string>(type: "TEXT", nullable: true),
                    NormalizedTextUpper = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Caption = table.Column<string>(type: "TEXT", nullable: true),
                    NormalizedCaptionUpper = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Chats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Messages_Users_FromId",
                        column: x => x.FromId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RolesUsers",
                columns: table => new
                {
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolesUsers", x => new { x.Role, x.UserId });
                    table.ForeignKey(
                        name: "FK_RolesUsers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chats_ChatTelegramId",
                table: "Chats",
                column: "ChatTelegramId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chats_FirstName",
                table: "Chats",
                column: "FirstName");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_IsForum",
                table: "Chats",
                column: "IsForum");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_LastName",
                table: "Chats",
                column: "LastName");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_LastUpdateUtc",
                table: "Chats",
                column: "LastUpdateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_NormalizedFirstNameUpper",
                table: "Chats",
                column: "NormalizedFirstNameUpper");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_NormalizedLastNameUpper",
                table: "Chats",
                column: "NormalizedLastNameUpper");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_NormalizedTitleUpper",
                table: "Chats",
                column: "NormalizedTitleUpper");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_NormalizedUsernameUpper",
                table: "Chats",
                column: "NormalizedUsernameUpper");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_Title",
                table: "Chats",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_Type",
                table: "Chats",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_Username",
                table: "Chats",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_JoinsUsersToChats_ChatId",
                table: "JoinsUsersToChats",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_JoinsUsersToChats_UserId_ChatId",
                table: "JoinsUsersToChats",
                columns: new[] { "UserId", "ChatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChatId",
                table: "Messages",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_FromId",
                table: "Messages",
                column: "FromId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_MessageTelegramId_ChatId_FromId",
                table: "Messages",
                columns: new[] { "MessageTelegramId", "ChatId", "FromId" });

            migrationBuilder.CreateIndex(
                name: "IX_RolesUsers_Role_UserId",
                table: "RolesUsers",
                columns: new[] { "Role", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolesUsers_UserId",
                table: "RolesUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_FirstName",
                table: "Users",
                column: "FirstName");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsBot",
                table: "Users",
                column: "IsBot");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastName",
                table: "Users",
                column: "LastName");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserTelegramId",
                table: "Users",
                column: "UserTelegramId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JoinsUsersToChats");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "RolesUsers");

            migrationBuilder.DropTable(
                name: "Chats");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
