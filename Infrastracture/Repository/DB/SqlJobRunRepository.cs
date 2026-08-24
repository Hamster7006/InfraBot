using InfraBot.Core.Interface.Repository;
using InfraBot.Entities;
using InfraBot.HelpData;
using InfraBot.Infrastracture.DataAccess;
using LinqToDB;
using LinqToDB.Async;

namespace InfraBot.Infrastracture.Repository.DB;

internal sealed class SqlJobRunRepository : IJobRunRepository
{
    private readonly IDataContextFactory<InfraDataContext> _factory;

    public SqlJobRunRepository(IDataContextFactory<InfraDataContext> factory)
    {
        _factory = factory;
    }

    public async Task<JobRun> CreateAsync(BotUser user, Script script, Server server, long chatId, CancellationToken ct)
    {
        var jobRun = new JobRun(script.Id, server.Id, user.Id, chatId);
        using var db = _factory.CreateDataContext();
        await db.InsertAsync(ModelMapper.MapToModel(jobRun), token: ct);
        return jobRun;
    }

    public async Task UpdateAsync(JobRun jobRun, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        await db.UpdateAsync(ModelMapper.MapToModel(jobRun), token: ct);
    }

    public async Task<JobRun?> GetAsync(Guid id, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        var model = await db.JobRuns.FirstOrDefaultAsync(x => x.Id == id, ct);
        return model is null ? null : ModelMapper.MapFromModel(model);
    }

    public async Task<IReadOnlyList<JobRun>> ReportAsync(bool allJobs, BotUser user, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        var since = DateTime.UtcNow.AddDays(-7);
        var query = db.JobRuns.Where(x => x.CreatedAt >= since);
        if (!allJobs)
            query = query.Where(x => x.InitiatedById == user.Id);

        var models = await query.ToListAsync(ct);
        return models.Select(ModelMapper.MapFromModel).ToList();
    }

    public async Task<IReadOnlyList<JobRun>> GetByIdsUserOrServer(Guid? userId, Guid? serverId, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        var query = db.JobRuns.AsQueryable();

        if (userId.HasValue && serverId.HasValue)
        {
            query = query.Where(x => x.InitiatedById == userId.Value || x.ServerId == serverId.Value);
        }
        else if (userId.HasValue)
        {
            query = query.Where(x => x.InitiatedById == userId.Value);
        }
        else if (serverId.HasValue)
        {
            query = query.Where(x => x.ServerId == serverId.Value);
        }

        var models = await query.ToListAsync(ct);
        return models.Select(ModelMapper.MapFromModel).ToList();
    }

    public async Task<IReadOnlyList<JobRun>> GetByUserIdAndServer(Guid userId, Guid? serverId, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        var query = db.JobRuns.Where(x => x.InitiatedById == userId);
        if (serverId.HasValue)
            query = query.Where(x => x.ServerId == serverId.Value);

        var models = await query.ToListAsync(ct);
        return models.Select(ModelMapper.MapFromModel).ToList();
    }

    public async Task<IReadOnlyList<JobRun>> GetByServerIdAndUser(Guid serverId, Guid? userId, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        var query = db.JobRuns.Where(x => x.ServerId == serverId);
        if (userId.HasValue)
            query = query.Where(x => x.InitiatedById == userId.Value);

        var models = await query.ToListAsync(ct);
        return models.Select(ModelMapper.MapFromModel).ToList();
    }

    public async Task DeleteByServerIdAsync(Guid serverId, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        await db.JobRuns.Where(x => x.ServerId == serverId).DeleteAsync(ct);
    }

    public async Task DeleteByScriptIdAsync(Guid scriptId, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        await db.JobRuns.Where(x => x.ScriptId == scriptId).DeleteAsync(ct);
    }
}
