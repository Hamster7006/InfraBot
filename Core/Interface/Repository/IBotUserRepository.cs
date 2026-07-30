using InfraBot.Entities;

namespace InfraBot.Core.Interface.Repository;

internal interface IBotUserRepository
{
    Task AddUserAsync(BotUser user, CancellationToken ct);
    Task UpdateUserAsync(BotUser user, CancellationToken ct);
    Task<BotUser?> GetUserAsync(Guid id, CancellationToken ct);
    Task<BotUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken ct);
    Task<IReadOnlyList<BotUser>> GetAllAsync(CancellationToken ct);
}
