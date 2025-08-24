////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Newtonsoft.Json;
using SharedLib;

namespace Transmission.Receives.storage;

/// <summary>
/// Read parameter
/// </summary>
public class ReadParameterReceive(IParametersStorage serializeStorageRepo)
    : IMQTTReceive<StorageMetadataModel?, TResponseModel<StorageCloudParameterPayloadModel>?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.ReadCloudParameterReceive;

    /// <inheritdoc/>
    public async Task<TResponseModel<StorageCloudParameterPayloadModel>?> ResponseHandleActionAsync(StorageMetadataModel? request, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await serializeStorageRepo.ReadParameterAsync(request, token);
    }
}