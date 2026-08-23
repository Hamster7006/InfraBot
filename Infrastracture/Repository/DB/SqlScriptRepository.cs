using InfraBot.Core.Interface.Repository;
using InfraBot.Entities;
using InfraBot.Infrastracture.DataAccess;
using LinqToDB;
using LinqToDB.Async;

namespace InfraBot.Infrastracture.Repository.DB;

internal sealed class SqlScriptRepository : IScriptRepository
{
    private readonly IDataContextFactory<InfraDataContext> _factory;

    public SqlScriptRepository(IDataContextFactory<InfraDataContext> factory)
    {
        _factory = factory;
    }

    public async Task AddAsync(Script script, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        await db.InsertAsync(ModelMapper.MapToModel(script), token: ct);
    }

    public async Task UpdateAsync(Script script, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        await db.UpdateAsync(ModelMapper.MapToModel(script), token: ct);
    }

    public async Task<Script?> GetAsync(Guid id, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        var model = await db.Scripts.FirstOrDefaultAsync(x => x.Id == id, ct);
        return model is null ? null : ModelMapper.MapFromModel(model);
    }

    public async Task<IReadOnlyList<Script>> GetAllAsync(CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        var models = await db.Scripts.ToListAsync(ct);
        return models.Select(ModelMapper.MapFromModel).ToList();
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        var lowerName = name.ToLowerInvariant();
        return await db.Scripts.AnyAsync(x => x.Name.ToLower() == lowerName, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        await db.Scripts.Where(x => x.Id == id).DeleteAsync(ct);
    }
}
