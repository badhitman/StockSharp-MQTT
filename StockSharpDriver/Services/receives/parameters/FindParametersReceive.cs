﻿////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Newtonsoft.Json;
using SharedLib;

namespace Transmission.Receives.storage;

/// <summary>
/// Find parameters
/// </summary>
public class FindParametersReceive(IParametersStorage serializeStorageRepo, ILogger<FindParametersReceive> LoggerRepo)
    : IMQTTReceive<RequestStorageBaseModel, TResponseModel<FoundParameterModel[]>>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.FindCloudParameterReceive;

    /// <inheritdoc/>
    public async Task<TResponseModel<FoundParameterModel[]>> ResponseHandleActionAsync(RequestStorageBaseModel request, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        LoggerRepo.LogDebug($"call `{GetType().Name}`: {JsonConvert.SerializeObject(request)}");
        return await serializeStorageRepo.FindAsync(request, token);
    }
}