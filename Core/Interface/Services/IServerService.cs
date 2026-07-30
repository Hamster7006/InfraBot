using InfraBot.Entities;
using InfraBot.Enums;

namespace InfraBot.Core.Interface.Services;

public interface IServerService
{
    Task<Server> AddServerAsync(Server server, CancellationToken ct);
    Task UpdateServerAsync(Server server, CancellationToken ct);
    Task<Server?> GetServerAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Server>> GetAccessibleServersAsync(BotUser user, CancellationToken ct);
    Task GrantAccessAsync(Guid serverId, Guid userId, CancellationToken ct);
    Task RevokeAccessAsync(Guid serverId, Guid userId, CancellationToken ct);
    Task SetScriptRequirementAsync(Guid serverId, Guid scriptId, UserStatus requiredRole, CancellationToken ct);
    Task RemoveScriptRequirementAsync(Guid serverId, Guid scriptId, CancellationToken ct);
}
