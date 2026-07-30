using InfraBot.Core.Interface.Repository;
using InfraBot.Entities;

namespace InfraBot.Infrastracture.Repository.Memory;

internal sealed class SvcSamAccountRepository : ISvcSamAccountRepository
{
    private readonly List<SvcSamAccount> _accounts = [];

    public Task AddAsync(SvcSamAccount svc, CancellationToken ct)
    {
        _accounts.Add(svc);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(SvcSamAccount svc, CancellationToken ct)
    {
        var index = _accounts.FindIndex(x => x.Id == svc.Id);
        if (index < 0)
            throw new KeyNotFoundException($"Учётная запись {svc.Id} не найдена.");

        _accounts[index] = svc;
        return Task.CompletedTask;
    }

    public Task<SvcSamAccount?> GetAsync(Guid id, CancellationToken ct)
        => Task.FromResult(_accounts.FirstOrDefault(x => x.Id == id));

    internal void LoadAll(IEnumerable<SvcSamAccount> accounts, bool replace)
    {
        if (replace)
            _accounts.Clear();

        _accounts.AddRange(accounts);
    }
}
