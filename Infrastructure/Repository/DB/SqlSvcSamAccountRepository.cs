using InfraBot.Core.Interface.Repository;
using InfraBot.Entities;
using InfraBot.Infrastructure.DataAccess;
using LinqToDB;
using LinqToDB.Async;

namespace InfraBot.Infrastructure.Repository.DB;

internal sealed class SqlSvcSamAccountRepository : ISvcSamAccountRepository
{
    private readonly IDataContextFactory<InfraDataContext> _factory;

    public SqlSvcSamAccountRepository(IDataContextFactory<InfraDataContext> factory)
    {
        _factory = factory;
    }

    public async Task AddAsync(SvcSamAccount svc, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        await db.InsertAsync(ModelMapper.MapToModel(svc), token: ct);
    }

    public async Task UpdateAsync(SvcSamAccount svc, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        await db.UpdateAsync(ModelMapper.MapToModel(svc), token: ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        await db.SvcSamAccounts.Where(x => x.Id == id).DeleteAsync(ct);
    }

    public async Task<SvcSamAccount?> GetAsync(Guid id, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        var model = await db.SvcSamAccounts.FirstOrDefaultAsync(x => x.Id == id, ct);
        return model is null ? null : ModelMapper.MapFromModel(model);
    }

    public async Task<IReadOnlyList<SvcSamAccount>> GetAllAsync(CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        var models = await db.SvcSamAccounts.ToListAsync(ct);
        return models.Select(ModelMapper.MapFromModel).ToList();
    }
}
