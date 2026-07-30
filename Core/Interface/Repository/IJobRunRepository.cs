using InfraBot.Entities;

namespace InfraBot.Core.Interface.Repository;

internal interface IJobRunRepository
{
    Task AddAsync(JobRun entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(JobRun entity, CancellationToken cancellationToken = default);
    Task<JobRun?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
