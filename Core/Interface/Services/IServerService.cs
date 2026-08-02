using InfraBot.Entities;

namespace InfraBot.Core.Interface.Services;

public interface IServerService
{
    Task<Server> AddServerAsync(Server server, CancellationToken ct);
    Task UpdateServerAsync(Server server, CancellationToken ct);
    Task<Server?> GetServerAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct);
    Task<IReadOnlyList<Server>> GetAccessibleServersAsync(BotUser user, CancellationToken ct);
    Task GrantAccessAsync(Guid serverId, Guid userId, CancellationToken ct);
    Task RevokeAccessAsync(Guid serverId, Guid userId, CancellationToken ct);
    Task AddScriptToServerAsync(Guid serverId, Guid scriptId, CancellationToken ct);
    Task RemoveScriptFromServerAsync(Guid serverId, Guid scriptId, CancellationToken ct);
    Task RemoveScriptFromAllServersAsync(Guid scriptId, CancellationToken ct);
    Task DeleteServerAsync(Guid serverId, CancellationToken ct);
    Task<IReadOnlyList<Server>> GetServersBySvcAccountAsync(Guid svcAccountId, CancellationToken ct);
    Task<IReadOnlyList<Server>> GetServersByScriptAsync(Guid scriptId, CancellationToken ct);
}
