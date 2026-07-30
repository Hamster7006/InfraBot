using InfraBot.Core.Interface.Repository;
using InfraBot.Entities;

namespace InfraBot.Infrastracture.Repository.Memory;

internal sealed class JobRunRepository : IJobRunRepository
{
    private readonly List<JobRun> _jobRuns = [];

    public Task<JobRun> CreateAsync(BotUser user, Script script, Server server, CancellationToken ct)
    {
        var jobRun = new JobRun(script.Id, server.Id, user.Id);
        _jobRuns.Add(jobRun);
        return Task.FromResult(jobRun);
    }

    public Task UpdateAsync(JobRun jobRun, CancellationToken ct)
    {
        var index = _jobRuns.FindIndex(x => x.Id == jobRun.Id);
        if (index < 0)
            throw new KeyNotFoundException($"Задача {jobRun.Id} не найдена.");

        _jobRuns[index] = jobRun;
        return Task.CompletedTask;
    }

    public Task<JobRun?> GetAsync(Guid id, CancellationToken ct)
        => Task.FromResult(_jobRuns.FirstOrDefault(x => x.Id == id));

    public Task<IReadOnlyList<JobRun>> GetByIdsUserOrServer(Guid? userId, Guid? serverId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<JobRun>>(_jobRuns
            .Where(x =>
                (userId.HasValue && x.InitiatedById == userId.Value) ||
                (serverId.HasValue && x.ServerId == serverId.Value))
            .ToList());

    public Task<IReadOnlyList<JobRun>> GetByUserIdAndServer(Guid userId, Guid? serverId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<JobRun>>(_jobRuns
            .Where(x =>
                x.InitiatedById == userId &&
                (!serverId.HasValue || x.ServerId == serverId.Value))
            .ToList());

    public Task<IReadOnlyList<JobRun>> GetByServerIdAndUser(Guid serverId, Guid? userId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<JobRun>>(_jobRuns
            .Where(x =>
                x.ServerId == serverId &&
                (!userId.HasValue || x.InitiatedById == userId.Value))
            .ToList());

    internal void LoadAll(IEnumerable<JobRun> jobRuns, bool replace)
    {
        if (replace)
            _jobRuns.Clear();

        _jobRuns.AddRange(jobRuns);
    }
}
