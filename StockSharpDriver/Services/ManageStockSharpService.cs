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
    public async Task<ResponseBaseModel> GenerateRegularCashFlowsAsync(CashFlowStockSharpRequestModel req, CancellationToken cancellationToken = default)
    {
        StockSharpAppContext ctx = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        InstrumentStockSharpModelDB instrumentDb;

        try
        {
            instrumentDb = await ctx.Instruments
                .Include(x => x.CashFlows)
                .Include(x => x.Markers)
                .FirstAsync(x => x.Id == req.InstrumentId, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            return ResponseBaseModel.CreateError(ex);
        }

        DateTime dt = instrumentDb.IssueDate;
        List<CashFlowModelDB> CashFlows = [];
        while (dt < instrumentDb.MaturityDate)
        {
            CashFlows.Add(new CashFlowModelDB()
            {
                StartDate = dt,
                EndDate = dt + TimeSpan.FromDays(req.FromDays),
                Coupon = Math.Round(1000 * instrumentDb.CouponRate * req.FromDays / 365, 2)
            });
            dt += TimeSpan.FromDays(req.FromDays);
        }

        CashFlows.RemoveAll(x => instrumentDb.CashFlows
            .Where(y => x.Coupon == x.Coupon)
            .Where(y => x.Notional == x.Notional)
            .Where(y => x.StartDate == x.StartDate)
            .Where(y => x.EndDate == x.EndDate)
            .Where(y => x.CouponRate == x.CouponRate)
        .Any());

        if (CashFlows.Count != 0)
        {
            await ctx.AddRangeAsync(CashFlows, cancellationToken);
            await ctx.SaveChangesAsync(cancellationToken);
            instrumentDb.CashFlows = await ctx.CashFlows
                .Where(x => x.InstrumentId == req.InstrumentId)
                .ToListAsync(cancellationToken: cancellationToken);
        }

        instrumentDb.CashFlows.First(s => s.EndDate.Equals(instrumentDb.MaturityDate)).Notional = req.NotionalFirst;

        return ResponseBaseModel.CreateSuccess("Ok");
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<FixMessageAdapterModelDB>> UpdateOrCreateAdapterAsync(FixMessageAdapterModelDB req, CancellationToken cancellationToken = default)
    {
        StockSharpAppContext ctx = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        TResponseModel<FixMessageAdapterModelDB> res = new();
        if (req.Id < 1)
        {
            req.Id = 0;
            req.IsOnline = false;
            req.CreatedAtUTC = DateTime.UtcNow;
            req.LastUpdatedAtUTC = DateTime.UtcNow;
            res.AddInfo("Создание нового адаптера");
            await ctx.AddAsync(req, cancellationToken);
            await ctx.SaveChangesAsync(cancellationToken);
            res.Response = req;
        }
        else
        {
            FixMessageAdapterModelDB ad = await ctx.Adapters.FirstOrDefaultAsync(x => x.Id == req.Id, cancellationToken: cancellationToken);
            if (ad is null)
            {
                res.AddError($"Адаптер #{req.Id} не найден");
                return res;
            }
            if (await ctx.Adapters.AnyAsync(x => x.Address == req.Address && x.AdapterTypeName == req.AdapterTypeName && x.IsOnline && x.Id != req.Id, cancellationToken: cancellationToken))
            {
                res.AddError($"Адаптер [{req.AdapterTypeName}] уже действует. Для активации данного адаптера - деактивируйте действующий");
                return res;
            }

            ad.SetUpdate(req);
            res.AddSuccess("Обновление адаптера");
            ctx.Update(ad);
            await ctx.SaveChangesAsync(cancellationToken);
            res.Response = ad;
        }

        return res;
    }

    /// <inheritdoc/>
    public async Task<TPaginationResponseModel<FixMessageAdapterModelDB>> AdaptersSelectAsync(TPaginationRequestStandardModel<AdaptersRequestModel> req, CancellationToken cancellationToken = default)
    {
        if (req.PageSize < 10)
            req.PageSize = 10;

        TPaginationResponseModel<FixMessageAdapterModelDB> res = new(req);
        StockSharpAppContext ctx = await toolsDbFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<FixMessageAdapterModelDB> q = ctx.Adapters.AsQueryable();

        if (req.Payload?.OnlineOnly is not null)
            q = q.Where(x => x.IsOnline == req.Payload.OnlineOnly);

        res.TotalRowsCount = await q.CountAsync(cancellationToken: cancellationToken);
        res.Response = await q
            .OrderBy(x => x.Name)
            .ThenBy(x => x.LastUpdatedAtUTC)
            .ThenBy(x => x.Id)
            .Skip(req.PageSize * req.PageNum)
            .Take(req.PageSize)
            .ToListAsync(cancellationToken: cancellationToken);

        return res;
    }

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> DeleteAdapterAsync(int adapterId, CancellationToken cancellationToken = default)
    {
        StockSharpAppContext ctx = await toolsDbFactory.CreateDbContextAsync(cancellationToken);

        ctx.RemoveRange(ctx.Adapters.Where(x => x.Id == adapterId));
        return await ctx.SaveChangesAsync(cancellationToken) == 0
            ? ResponseBaseModel.CreateWarning($"Адаптер #{adapterId} отсутствует (не найден)")
            : ResponseBaseModel.CreateSuccess($"Адаптер #{adapterId} удалён");
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<FixMessageAdapterModelDB[]>> AdaptersGetAsync(int[] req, CancellationToken cancellationToken = default)
    {
        TResponseModel<FixMessageAdapterModelDB[]> res = new();

        StockSharpAppContext ctx = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<FixMessageAdapterModelDB> q = ctx.Adapters.AsQueryable();

        if (req is null || req.Length == 0)
            res.Response = await q.ToArrayAsync(cancellationToken: cancellationToken);
        else
            res.Response = await q.Where(x => req.Contains(x.Id)).ToArrayAsync(cancellationToken: cancellationToken);

        return res;
    }

    /// <inheritdoc/>
    public async Task<TPaginationResponseModel<OrderStockSharpViewModel>> OrdersSelectAsync(TPaginationRequestStandardModel<OrdersSelectStockSharpRequestModel> req, CancellationToken cancellationToken = default)
    {
        if (req.PageSize < 10)
            req.PageSize = 10;

        TPaginationResponseModel<OrderStockSharpModelDB> res = new(req);
        StockSharpAppContext ctx = await toolsDbFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<OrderStockSharpModelDB> q = ctx.Orders.AsQueryable();

        res.TotalRowsCount = await q.CountAsync(cancellationToken: cancellationToken);
        res.Response = await q
            .OrderBy(x => x.CreatedAtUTC)
            .ThenBy(x => x.LastUpdatedAtUTC)
            .ThenBy(x => x.Id)
            .Skip(req.PageSize * req.PageNum)
            .Take(req.PageSize)
            .Include(x => x.Instrument)
            .Include(x => x.Portfolio)
            .ToListAsync(cancellationToken: cancellationToken);

        return new(req)
        {
            TotalRowsCount = res.TotalRowsCount,
            Response = [.. res.Response.Select(x => (OrderStockSharpViewModel)x)]
        };
    }

    /// <inheritdoc/>
    public Task<AboutDatabasesResponseModel> AboutDatabases(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AboutDatabasesResponseModel()
        {
            DriverDatabase = $"{StockSharpAppLayerContext.DbPath}",
            PropertiesDatabase = $"{PropertiesStorageLayerContext.DbPath}"
        });
    }
}