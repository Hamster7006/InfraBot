using InfraBot.Entities;

namespace InfraBot.Core.Interface.Repository;

internal interface IJobRunRepository
{
    Task<JobRun> CreateAsync(BotUser user, Script script, Server server, long chatId, CancellationToken ct);
    Task UpdateAsync(JobRun jobRun, CancellationToken ct);
    Task<JobRun?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<JobRun>> GetByIdsUserOrServer(Guid? userId, Guid? serverId, CancellationToken ct);
    Task<IReadOnlyList<JobRun>> GetByUserIdAndServer(Guid userId, Guid? serverId, CancellationToken ct);
    Task<IReadOnlyList<JobRun>> GetByServerIdAndUser(Guid serverId, Guid? userId, CancellationToken ct);
    Task DeleteByServerIdAsync(Guid serverId, CancellationToken ct);
    Task DeleteByScriptIdAsync(Guid scriptId, CancellationToken ct);
    /// <param name="allJobs">true — все запуски; false — только запуски текущего пользователя.</param>
    Task<IReadOnlyList<JobRun>> ReportAsync(bool allJobs, BotUser user, CancellationToken ct);

}
