using InfraBot.Core.Interface.Repository;
using InfraBot.Core.Interface.Services;
using InfraBot.Entities;

namespace InfraBot.Infrastracture.Services;

internal sealed class SvcSamAccountService : ISvcSamAccountService
{
    private readonly ISvcSamAccountRepository _accounts;

    public SvcSamAccountService(ISvcSamAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public Task<SvcSamAccount> AddAsync(SvcSamAccount account, CancellationToken ct)
    {
        return AddInternalAsync(account, ct);
    }

    public Task UpdateAsync(SvcSamAccount account, CancellationToken ct)
        => _accounts.UpdateAsync(account, ct);

    public Task<SvcSamAccount?> GetAsync(Guid id, CancellationToken ct)
        => _accounts.GetAsync(id, ct);

    private async Task<SvcSamAccount> AddInternalAsync(SvcSamAccount account, CancellationToken ct)
    {
        await _accounts.AddAsync(account, ct);
        return account;
    }
}
