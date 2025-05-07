////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;
using SharedLib;
using DbcLib;

namespace StockSharpDriver;

public class ManageStockSharpService(IDbContextFactory<StockSharpAppContext> toolsDbFactory) : IManageStockSharpService
{
    /// <inheritdoc/>
    public async Task<TResponseModel<int>> UpdateOrCreateAdapterAsync(FixMessageAdapterModelDB req, CancellationToken cancellationToken = default)
    {
        StockSharpAppContext ctx = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        FixMessageAdapterModelDB ad = ctx.Adapters.FirstOrDefault(x => x.Id == req.Id);
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<TPaginationResponseModel<FixMessageAdapterModelDB>> AdaptersSelectAsync(TPaginationRequestStandardModel<AdaptersRequestModel> req, CancellationToken cancellationToken = default)
    {
        StockSharpAppContext ctx = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> DeleteAdapterAsync(FixMessageAdapterModelDB req, CancellationToken cancellationToken = default)
    {
        StockSharpAppContext ctx = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        throw new NotImplementedException();
    }
}