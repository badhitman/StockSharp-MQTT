////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// Поиск Board`s, подходящие под запрос
/// </summary>
public class FindBoardsStockSharpDriverReceive(IDataStockSharpService ssRepo)
    : IMQTTReceive<BoardStockSharpModel?, TResponseModel<List<BoardStockSharpMetaModel>>?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.FindBoardsStockSharpReceive;

    /// <inheritdoc/>
    public async Task<TResponseModel<List<BoardStockSharpMetaModel>>?> ResponseHandleActionAsync(BoardStockSharpModel? req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.FindBoardsAsync(req, token);
    }
}