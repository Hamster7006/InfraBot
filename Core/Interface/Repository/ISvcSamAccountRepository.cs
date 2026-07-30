using InfraBot.Entities;

namespace InfraBot.Core.Interface.Repository;

internal interface ISvcSamAccountRepository
{
    Task AddAsync(SvcSamAccount svc, CancellationToken ct);
    Task UpdateAsync(SvcSamAccount svc, CancellationToken ct);
    Task<SvcSamAccount?> GetAsync(Guid id, CancellationToken ct);
}
