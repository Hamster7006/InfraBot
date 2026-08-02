using InfraBot.Entities;
using Telegram.Bot.Types;

namespace InfraBot.Core.Interface.Services;

public interface IJobRunService
{
    Task<JobRun> CreateJobAsync(BotUser user, Guid scriptId, Guid serverId, Chat chat, CancellationToken ct);
    Task<JobRun?> GetJobAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<JobRun>> GetJobsForUserAsync(Guid userId, Guid? serverId, CancellationToken ct);
    Task DeleteJobsByServerAsync(Guid serverId, CancellationToken ct);
    Task DeleteJobsByScriptAsync(Guid scriptId, CancellationToken ct);
    /// <param name="allJobs">true — все запуски; false — только запуски текущего пользователя.</param>
    Task<IReadOnlyList<JobRun>> ReportAsync(bool allJobs, BotUser user, CancellationToken ct);
}
