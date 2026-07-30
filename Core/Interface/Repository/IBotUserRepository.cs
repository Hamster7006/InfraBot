using InfraBot.Entities;

namespace InfraBot.Core.Interface.Repository;

internal interface IBotUserRepository
{
    Task AddAsync(BotUser user, CancellationToken ct);
    Task UpdateAsync(BotUser user, CancellationToken ct);
    Task<BotUser?> GetAsync(Guid id, CancellationToken ct);
}
