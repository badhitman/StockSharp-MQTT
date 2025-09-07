////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Newtonsoft.Json;
using SharedLib;
using StockSharpDriver;

namespace Transmission.Receives.telegram;

/// <summary>
/// UserTelegramPermissionUpdateReceive
/// </summary>
public class UserTelegramPermissionUpdateReceive(ITelegramBotService tgRepo, ILogger<UserTelegramPermissionUpdateReceive> _logger)
    : IMQTTReceive<UserTelegramPermissionSetModel?, ResponseBaseModel?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.UserTelegramPermissionUpdateReceive;

    /// <inheritdoc/>
    public async Task<ResponseBaseModel?> ResponseHandleActionAsync(UserTelegramPermissionSetModel? req, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        _logger.LogInformation($"call `{GetType().Name}`: {JsonConvert.SerializeObject(req)}");
        return await tgRepo.UserTelegramPermissionUpdateAsync(req, token);
    }
}