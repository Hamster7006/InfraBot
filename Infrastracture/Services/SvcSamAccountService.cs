using InfraBot.Core.Exceptions;
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

    public async Task<SvcSamAccount> AddAsync(SvcSamAccount account, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(account.SamAccountName))
            throw new InfraBotException("Логин учётной записи не может быть пустым.");

        if (string.IsNullOrWhiteSpace(account.Password))
            throw new InfraBotException("Пароль не может быть пустым.");

        if (await ExistsBySamAccountNameAsync(account.SamAccountName, null, ct))
            throw new InfraBotException($"Учётная запись «{account.SamAccountName}» уже существует.");

        await _accounts.AddAsync(account, ct);
        return account;
    }

    public async Task UpdateAsync(SvcSamAccount account, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(account.SamAccountName))
            throw new InfraBotException("Логин учётной записи не может быть пустым.");

        if (string.IsNullOrWhiteSpace(account.Password))
            throw new InfraBotException("Пароль не может быть пустым.");

        await GetAccountOrThrow(account.Id, ct);

        if (await ExistsBySamAccountNameAsync(account.SamAccountName, account.Id, ct))
            throw new InfraBotException($"Учётная запись «{account.SamAccountName}» уже существует.");

        await _accounts.UpdateAsync(account, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        await GetAccountOrThrow(id, ct);
        await _accounts.DeleteAsync(id, ct);
    }

    public Task<SvcSamAccount?> GetAsync(Guid id, CancellationToken ct)
        => _accounts.GetAsync(id, ct);

    public Task<IReadOnlyList<SvcSamAccount>> GetAllAsync(CancellationToken ct)
        => _accounts.GetAllAsync(ct);

    public async Task<bool> ExistsBySamAccountNameAsync(string samAccountName, Guid? excludeId, CancellationToken ct)
    {
        var all = await _accounts.GetAllAsync(ct);
        return all.Any(x =>
            string.Equals(x.SamAccountName, samAccountName, StringComparison.OrdinalIgnoreCase)
            && (!excludeId.HasValue || x.Id != excludeId.Value));
    }

    private async Task<SvcSamAccount> GetAccountOrThrow(Guid id, CancellationToken ct)
        => await _accounts.GetAsync(id, ct)
            ?? throw new InfraBotException($"Учётная запись {id} не найдена.");
}
