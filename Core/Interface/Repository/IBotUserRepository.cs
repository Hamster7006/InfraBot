using InfraBot.Entities;

namespace InfraBot.Core.Interface.Repository;

internal interface IBotUserRepository
{
    Task AddAsync(BotUser entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(BotUser entity, CancellationToken cancellationToken = default);
    Task<BotUser?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
