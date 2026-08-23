using InfraBot.Core.DataAccess.Models;
using InfraBot.Core.Interface.Repository;
using InfraBot.Entities;
using InfraBot.Infrastracture.DataAccess;
using LinqToDB;
using LinqToDB.Async;

namespace InfraBot.Infrastracture.Repository.DB;

internal sealed class SqlServerRepository : IServerRepository
{
    private readonly IDataContextFactory<InfraDataContext> _factory;

    public SqlServerRepository(IDataContextFactory<InfraDataContext> factory)
    {
        _factory = factory;
    }

    public async Task AddAsync(Server server, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        await using var transaction = await db.BeginTransactionAsync(ct);
        try
        {
            await db.InsertAsync(ModelMapper.MapToModel(server), token: ct);
            await SyncServerCollectionsAsync(db, server, ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task UpdateAsync(Server server, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        await using var transaction = await db.BeginTransactionAsync(ct);
        try
        {
            await db.UpdateAsync(ModelMapper.MapToModel(server), token: ct);
            await SyncServerCollectionsAsync(db, server, ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<Server?> GetAsync(Guid id, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        var model = await db.Servers.FirstOrDefaultAsync(x => x.Id == id, ct);
        return await MapServerAsync(db, model, ct);
    }

    public async Task<Server?> GetAsync(string name, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        var lowerName = name.ToLowerInvariant();
        var model = await db.Servers.FirstOrDefaultAsync(x => x.ServerName.ToLower() == lowerName, ct);
        return await MapServerAsync(db, model, ct);
    }

    public async Task<IReadOnlyList<Server>> GetAllAsync(CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        var models = await db.Servers.ToListAsync(ct);
        var result = new List<Server>(models.Count);
        foreach (var model in models)
        {
            var server = await MapServerAsync(db, model, ct);
            if (server is not null)
                result.Add(server);
        }

        return result;
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        var lowerName = name.ToLowerInvariant();
        return await db.Servers.AnyAsync(x => x.ServerName.ToLower() == lowerName, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        await db.Servers.Where(x => x.Id == id).DeleteAsync(ct);
    }

    private static async Task<Server?> MapServerAsync(
        InfraDataContext db,
        ServerModel? model,
        CancellationToken ct)
    {
        if (model is null)
            return null;

        var scriptIds = await db.ServerScriptRequirements
            .Where(x => x.ServerId == model.Id)
            .Select(x => x.ScriptId)
            .ToListAsync(ct);

        var userIds = await db.ServerGrantedUsers
            .Where(x => x.ServerId == model.Id)
            .Select(x => x.UserId)
            .ToListAsync(ct);

        return ModelMapper.MapFromModel(model, scriptIds, userIds);
    }

    private static async Task SyncServerCollectionsAsync(
        InfraDataContext db,
        Server server,
        CancellationToken ct)
    {
        await db.ServerScriptRequirements
            .Where(x => x.ServerId == server.Id)
            .DeleteAsync(ct);

        await db.ServerGrantedUsers
            .Where(x => x.ServerId == server.Id)
            .DeleteAsync(ct);

        foreach (var scriptId in server.ScriptRequirements)
        {
            await db.InsertAsync(
                new ServerScriptRequirementModel { ServerId = server.Id, ScriptId = scriptId },
                token: ct);
        }

        foreach (var userId in server.GrantedUserIds)
        {
            await db.InsertAsync(
                new ServerGrantedUserModel { ServerId = server.Id, UserId = userId },
                token: ct);
        }
    }
}
