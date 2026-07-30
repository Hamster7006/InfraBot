using InfraBot.Entities;

namespace InfraBot.Core.Interface.Repository;

internal interface ISvcSamAccountRepository
{
    Task AddAsync(SvcSamAccount entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(SvcSamAccount entity, CancellationToken cancellationToken = default);
    Task<SvcSamAccount?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
