using InfraBot.Core.Exceptions;
using InfraBot.Core.Interface.Repository;
using InfraBot.Core.Interface.Services;
using InfraBot.Entities;
using InfraBot.Enums;

namespace InfraBot.Infrastracture.Services;

internal sealed class BotUserService : IBotUserService
{
    private readonly IBotUserRepository _users;

    public BotUserService(IBotUserRepository users)
    {
        _users = users;
    }

    public async Task<BotUser> RegisterUserAsync(long telegramUserId, string telegramUserName, CancellationToken ct)
    {
        var existing = await _users.GetUserByTelegramUserIdAsync(telegramUserId, ct);
        if (existing is not null)
            return existing;

        var user = new BotUser(telegramUserId, telegramUserName);
        await _users.AddUserAsync(user, ct);
        return user;
    }

    public Task<BotUser?> GetUserAsync(long telegramUserId, CancellationToken ct)
        => _users.GetUserByTelegramUserIdAsync(telegramUserId, ct);

    public Task<BotUser?> GetUserByIdAsync(Guid id, CancellationToken ct)
        => _users.GetUserAsync(id, ct);

    public async Task SetUserStatusAsync(Guid userId, UserStatus status, CancellationToken ct)
    {
        var user = await GetUserOrThrow(userId, ct);
        user.Status = status;
        // Смена роли закрывает активную заявку на повышение
        user.Pending = UserPending.None;
        await _users.UpdateUserAsync(user, ct);
    }

    /// <summary>Ставит заявку в очередь без уведомлений — только флаг Pending.</summary>
    public async Task RequestElevationAsync(Guid userId, CancellationToken ct)
    {
        var user = await GetUserOrThrow(userId, ct);

        if (user.Pending == UserPending.Pending)
            throw new InfraBotException("Запрос на повышение уже отправлен.");

        if (ResolveNextRole(user.Status) is null)
            throw new InfraBotException("Текущая роль не позволяет запросить повышение.");

        user.Pending = UserPending.Pending;
        await _users.UpdateUserAsync(user, ct);
    }

    public async Task ApproveElevationAsync(Guid userId, CancellationToken ct)
    {
        var user = await GetUserOrThrow(userId, ct);

        var targetStatus = ResolveElevationTarget(user)
            ?? throw new InfraBotException("У пользователя нет активного запроса на повышение.");

        user.Status = targetStatus;
        user.Pending = UserPending.None;
        await _users.UpdateUserAsync(user, ct);
    }

    public async Task RejectElevationAsync(Guid userId, CancellationToken ct)
    {
        var user = await GetUserOrThrow(userId, ct);

        if (user.Pending != UserPending.Pending)
            throw new InfraBotException("У пользователя нет активного запроса на повышение.");

        user.Pending = UserPending.None;
        await _users.UpdateUserAsync(user, ct);
    }

    public async Task<IReadOnlyList<BotUser>> GetPendingElevationRequestsAsync(CancellationToken ct)
    {
        var all = await _users.GetAllAsync(ct);
        return all.Where(x => x.Pending == UserPending.Pending).ToList();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<BotUser>> GetAllUsersAsync(CancellationToken ct)
        => _users.GetAllAsync(ct);

    /// <summary>
    /// Целевая роль при Pending = Pending: Guest → Operator.
    /// </summary>
    private static UserStatus? ResolveElevationTarget(BotUser user)
    {
        if (user.Pending != UserPending.Pending)
            return null;

        return ResolveNextRole(user.Status);
    }

    private static UserStatus? ResolveNextRole(UserStatus current) =>
        current switch
        {
            UserStatus.Guest => UserStatus.Operator,
            _ => null
        };

    private async Task<BotUser> GetUserOrThrow(Guid userId, CancellationToken ct)
        => await _users.GetUserAsync(userId, ct)
            ?? throw new InfraBotException($"Пользователь {userId} не найден.");
}
