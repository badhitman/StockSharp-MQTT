////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;

namespace SharedLib;

/// <summary>
/// RoleUserTelegramModelDB
/// </summary>
[Index(nameof(Role))]
public class RoleUserTelegramModelDB : RoleUserTelegramViewModel
{
    public new UserTelegramModelDB? User { get; set; }
}