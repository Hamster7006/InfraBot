using InfraBot.Entities;

namespace InfraBot.Core.Interface.Repository;

internal interface IJobRunRepository
{
    Task<JobRun> CreateAsync(BotUser user, Script script, Server server, CancellationToken ct);
    Task UpdateAsync(JobRun jobRun, CancellationToken ct);
    Task<JobRun?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<JobRun>> GetByIdsUserOrServer(Guid? userId, Guid? serverId, CancellationToken ct);
    Task<IReadOnlyList<JobRun>> GetByUserIdAndServer(Guid userId, Guid? serverId, CancellationToken ct);
    Task<IReadOnlyList<JobRun>> GetByServerIdAndUser(Guid serverId, Guid? userId, CancellationToken ct);

}
