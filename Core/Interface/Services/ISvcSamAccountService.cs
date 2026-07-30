using InfraBot.Entities;

namespace InfraBot.Core.Interface.Services;

public interface ISvcSamAccountService
{
    Task<SvcSamAccount> AddAsync(SvcSamAccount account, CancellationToken ct);
    Task UpdateAsync(SvcSamAccount account, CancellationToken ct);
    Task<SvcSamAccount?> GetAsync(Guid id, CancellationToken ct);
}
