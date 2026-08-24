using InfraBot.Core.Interface.Services;
using InfraBot.Entities;

namespace InfraBot.Infrastructure.Services;

internal sealed class UserInfoReportService : IUserInfoReportService
{
    private readonly IServerService _servers;
    private readonly IJobRunService _jobRuns;

    public UserInfoReportService(IServerService servers, IJobRunService jobRuns)
    {
        _servers = servers;
        _jobRuns = jobRuns;
    }

    public async Task<UserInfoReport> BuildAsync(BotUser user, CancellationToken ct)
    {
        var accessibleServers = await _servers.GetAccessibleServersAsync(user, ct);
        var jobRuns = await _jobRuns.GetJobsForUserAsync(user.Id, serverId: null, ct);

        return new UserInfoReport
        {
            AccessibleServersCount = accessibleServers.Count,
            // TODO: уточнить критерии отчёта (все JobRun / только успешные / за период)
            JobRunsCount = jobRuns.Count
        };
    }
}
