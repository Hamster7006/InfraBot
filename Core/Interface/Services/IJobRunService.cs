using InfraBot.Entities;

namespace InfraBot.Core.Interface.Services;

public interface IJobRunService
{
    Task<JobRun> CreateJobAsync(BotUser user, Guid scriptId, Guid serverId, CancellationToken ct);
    Task<JobRun?> GetJobAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<JobRun>> GetJobsForUserAsync(Guid userId, Guid? serverId, CancellationToken ct);
}
