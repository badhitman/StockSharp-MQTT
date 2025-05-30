////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// OrderRegisterRequestStockSharpDriverReceive
/// </summary>
public class OrderRegisterRequestStockSharpDriverReceive(IDriverStockSharpService ssRepo)
    : IMQTTReceive<OrderRegisterRequestModel, OrderRegisterRequestResponseModel>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.OrderRegisterRequestStockSharpReceive;

    /// <inheritdoc/>
    public async Task<OrderRegisterRequestResponseModel> ResponseHandleActionAsync(OrderRegisterRequestModel req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.OrderRegisterRequestAsync(req, token);
    }
}