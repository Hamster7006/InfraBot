using InfraBot.Entities;

namespace InfraBot.Core.Interface.Repository;

internal interface IJobRunRepository
{
    Task AddAsync(JobRun jobRun, CancellationToken ct);
    Task UpdateAsync(JobRun jobRun, CancellationToken ct);
    Task<JobRun?> GetAsync(Guid id, CancellationToken ct);
}
