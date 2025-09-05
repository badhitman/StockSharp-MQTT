////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;

namespace SharedLib;

/// <summary>
/// RoleUserTelegramModelDB
/// </summary>
[Index(nameof(Role), nameof(UserId), IsUnique = true)]
public class RoleUserTelegramModelDB
{
    public TelegramUsersRolesEnum Role { get; set; }

    public UserTelegramModelDB? User { get; set; }

    public int UserId { get; set; }
}
