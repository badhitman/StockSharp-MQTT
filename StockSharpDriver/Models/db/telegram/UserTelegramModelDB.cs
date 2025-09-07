////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;

namespace SharedLib;

/// <summary>
/// UserTelegramModelDB
/// </summary>
[Index(nameof(UserTelegramId), IsUnique = true)]
[Index(nameof(Username)), Index(nameof(FirstName)), Index(nameof(LastName)), Index(nameof(IsBot))]
public class UserTelegramModelDB : UserTelegramViewModel
{
    /// <inheritdoc/>
    public List<MessageTelegramModelDB>? Messages { get; set; }

    /// <inheritdoc/>
    public new List<JoinUserChatModelDB>? ChatsJoins { get; set; }

    /// <inheritdoc/>
    public new List<RoleUserTelegramModelDB>? UserRoles { get; set; }
}