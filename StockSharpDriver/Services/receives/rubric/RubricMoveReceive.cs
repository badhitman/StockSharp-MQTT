////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Newtonsoft.Json;
using SharedLib;

namespace Transmission.Receives.rubrics;

/// <summary>
/// Сдвинуть рубрику
/// </summary>
public class RubricMoveReceive(IRubricsService hdRepo, ILogger<RubricMoveReceive> loggerRepo)
    : IMQTTReceive<TAuthRequestModel<RowMoveModel>?, ResponseBaseModel?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.RubricForIssuesMoveHelpDeskReceive;

    /// <inheritdoc/>
    public async Task<ResponseBaseModel?> ResponseHandleActionAsync(TAuthRequestModel<RowMoveModel>? req, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        loggerRepo.LogInformation($"call `{GetType().Name}`: {JsonConvert.SerializeObject(req)}");
        return await hdRepo.RubricMoveAsync(req, token);
    }
}