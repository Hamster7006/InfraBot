using InfraBot.Core.Interface.Repository;
using InfraBot.Entities;

namespace InfraBot.Infrastracture.Repository.Memory;

internal sealed class BotUserRepository : IBotUserRepository
{
    private readonly List<BotUser> _users = [];

    public Task AddUserAsync(BotUser user, CancellationToken ct)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateUserAsync(BotUser user, CancellationToken ct)
    {
        var index = _users.FindIndex(x => x.Id == user.Id);
        if (index < 0)
            throw new KeyNotFoundException($"Пользователь {user.Id} не найден.");

        _users[index] = user;
        return Task.CompletedTask;
    }

    public Task<BotUser?> GetUserAsync(Guid id, CancellationToken ct)
        => Task.FromResult(_users.FirstOrDefault(x => x.Id == id));

    public Task<BotUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken ct)
        => Task.FromResult(_users.FirstOrDefault(x => x.TelegramId == telegramUserId));

    public Task<IReadOnlyList<BotUser>> GetAllAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<BotUser>>(_users.ToList());

    internal void LoadAll(IEnumerable<BotUser> users, bool replace)
    {
        if (replace)
            _users.Clear();

        _users.AddRange(users);
    }
}
