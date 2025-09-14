////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;
using SharedLib;
using DbcLib;

namespace StockSharpDriver;

/// <summary>
/// EventsNotifyStockSharpTransmission
/// </summary>
public class EventsNotifyStockSharpTransmission(IDbContextFactory<TelegramBotAppContext> toolsDbFactory, ITelegramBotService tgRepo) : IEventsNotify
{
    /// <inheritdoc/>
    public async Task<ResponseBaseModel> ToastClientShow(ToastShowClientModel req, CancellationToken cancellationToken = default)
    {
        TelegramBotAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<UserTelegramModelDB> q;

        switch (req.TypeMessage)
        {
            case MessagesTypesEnum.Info:
                q = context.Users.Where(x => context.RolesUsers.Any(y => y.UserId == x.Id && y.Role == TelegramUsersRolesEnum.NotifyToastInfo));
                break;
            case MessagesTypesEnum.Error:
                q = context.Users.Where(x => context.RolesUsers.Any(y => y.UserId == x.Id && y.Role == TelegramUsersRolesEnum.NotifyToastError));
                break;
            case MessagesTypesEnum.Success:
                q = context.Users.Where(x => context.RolesUsers.Any(y => y.UserId == x.Id && y.Role == TelegramUsersRolesEnum.NotifyToastSuccess));
                break;
            case MessagesTypesEnum.Warning:
                q = context.Users.Where(x => context.RolesUsers.Any(y => y.UserId == x.Id && y.Role == TelegramUsersRolesEnum.NotifyToastWarning));
                break;
            default:
                return ResponseBaseModel.CreateError($"Error detect role: {req.TypeMessage}");
        }

        List<UserTelegramModelDB> usersDb = await q.ToListAsync(cancellationToken: cancellationToken);
        if (usersDb.Count == 0)
            return ResponseBaseModel.CreateInfo("Users for sent not exists");

        ResponseBaseModel res = new();
        await Task.WhenAll(usersDb.Select(u => Task.Run(async () =>
        {
            TResponseModel<MessageComplexIdsModel> msg = await tgRepo.SendTextMessageTelegramAsync(new SendTextMessageTelegramBotModel()
            {
                From = req.HeadTitle,
                Message = req.MessageText,
                UserTelegramId = u.UserTelegramId,
            });

            if (!msg.Success())
                res.AddError($"error sending tg message: {msg.Message()}");
            else
                res.AddSuccess($"sent to: {u.GetName()}");
        })));

        return res;
    }
}