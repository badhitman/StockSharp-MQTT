////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Newtonsoft.Json;
using SharedLib;

namespace Transmission.Receives.storage;

/// <summary>
/// Delete parameter
/// </summary>
public class DeleteParameterReceive(IParametersStorage serializeStorageRepo, ILogger<DeleteParameterReceive> LoggerRepo)
    : IMQTTReceive<StorageMetadataModel?, ResponseBaseModel?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.DeleteCloudParameterReceive;

    /// <inheritdoc/>
    public async Task<ResponseBaseModel?> ResponseHandleActionAsync(StorageMetadataModel? req, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        req.Normalize();
        LoggerRepo.LogDebug($"call `{GetType().Name}`: {JsonConvert.SerializeObject(req)}");

        return await serializeStorageRepo.DeleteParameter(req, token);
    }
}