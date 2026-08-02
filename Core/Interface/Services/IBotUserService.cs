using InfraBot.Entities;
using InfraBot.Enums;

namespace InfraBot.Core.Interface.Services;

public interface IBotUserService
{
    Task<BotUser> RegisterUserAsync(long telegramUserId, string telegramUserName, CancellationToken ct);
    Task<BotUser?> GetUserAsync(long telegramUserId, CancellationToken ct);
    Task<BotUser?> GetUserByIdAsync(Guid id, CancellationToken ct);
    Task SetUserStatusAsync(Guid userId, UserStatus status, CancellationToken ct);
    Task RequestElevationAsync(Guid userId, CancellationToken ct);
    Task ApproveElevationAsync(Guid userId, CancellationToken ct);
    Task RejectElevationAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<BotUser>> GetPendingElevationRequestsAsync(CancellationToken ct);

    /// <summary>Все пользователи — для списка /usercontrol.</summary>
    Task<IReadOnlyList<BotUser>> GetAllUsersAsync(CancellationToken ct);
}
