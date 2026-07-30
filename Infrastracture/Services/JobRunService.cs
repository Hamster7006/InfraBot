using InfraBot.Core.Exceptions;
using InfraBot.Core.Interface.Repository;
using InfraBot.Core.Interface.Services;
using InfraBot.Entities;

namespace InfraBot.Infrastracture.Services;

internal sealed class JobRunService : IJobRunService
{
    private readonly IJobRunRepository _jobRuns;
    private readonly IServerRepository _servers;
    private readonly IScriptRepository _scripts;

    public JobRunService(
        IJobRunRepository jobRuns,
        IServerRepository servers,
        IScriptRepository scripts)
    {
        _jobRuns = jobRuns;
        _servers = servers;
        _scripts = scripts;
    }

    public async Task<JobRun> CreateJobAsync(BotUser user, Guid scriptId, Guid serverId, CancellationToken ct)
    {
        var server = await _servers.GetAsync(serverId, ct)
            ?? throw new InfraBotException($"Сервер {serverId} не найден.");

        var script = await _scripts.GetAsync(scriptId, ct)
            ?? throw new InfraBotException($"Скрипт {scriptId} не найден.");

        if (!AccessRules.CanRunScript(user, server, script))
            throw new InfraBotException("Недостаточно прав для запуска скрипта на этом сервере.");

        return await _jobRuns.CreateAsync(user, script, server, ct);
    }

    public Task<JobRun?> GetJobAsync(Guid id, CancellationToken ct)
        => _jobRuns.GetAsync(id, ct);

    public Task<IReadOnlyList<JobRun>> GetJobsForUserAsync(Guid userId, Guid? serverId, CancellationToken ct)
        => _jobRuns.GetByUserIdAndServer(userId, serverId, ct);
}
