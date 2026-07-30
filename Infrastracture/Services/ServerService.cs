using InfraBot.Core.Exceptions;
using InfraBot.Core.Interface.Repository;
using InfraBot.Core.Interface.Services;
using InfraBot.Entities;
using InfraBot.Enums;

namespace InfraBot.Infrastracture.Services;

internal sealed class ServerService : IServerService
{
    private readonly IServerRepository _servers;

    public ServerService(IServerRepository servers)
    {
        _servers = servers;
    }

    public async Task<Server> AddServerAsync(Server server, CancellationToken ct)
    {
        if (await _servers.ExistsByNameAsync(server.ServerName, ct))
            throw new InfraBotException($"Сервер «{server.ServerName}» уже существует.");

        await _servers.AddAsync(server, ct);
        return server;
    }

    public Task UpdateServerAsync(Server server, CancellationToken ct)
        => _servers.UpdateAsync(server, ct);

    public Task<Server?> GetServerAsync(Guid id, CancellationToken ct)
        => _servers.GetAsync(id, ct);

    public async Task<IReadOnlyList<Server>> GetAccessibleServersAsync(BotUser user, CancellationToken ct)
    {
        var all = await _servers.GetAllAsync(ct);

        if (user.Status == UserStatus.Admin)
            return all;

        return all.Where(x => x.GrantedUserIds.Contains(user.Id)).ToList();
    }

    public async Task GrantAccessAsync(Guid serverId, Guid userId, CancellationToken ct)
    {
        var server = await GetServerOrThrow(serverId, ct);
        if (!server.GrantedUserIds.Contains(userId))
            server.GrantedUserIds.Add(userId);

        await _servers.UpdateAsync(server, ct);
    }

    public async Task RevokeAccessAsync(Guid serverId, Guid userId, CancellationToken ct)
    {
        var server = await GetServerOrThrow(serverId, ct);
        server.GrantedUserIds.Remove(userId);
        await _servers.UpdateAsync(server, ct);
    }

    public async Task SetScriptRequirementAsync(Guid serverId, Guid scriptId, UserStatus requiredRole, CancellationToken ct)
    {
        if (requiredRole is UserStatus.Blocked or UserStatus.Guest)
            throw new InfraBotException("Недопустимая роль для запуска скрипта.");

        var server = await GetServerOrThrow(serverId, ct);
        server.ScriptRequirements[scriptId] = requiredRole;
        await _servers.UpdateAsync(server, ct);
    }

    public async Task RemoveScriptRequirementAsync(Guid serverId, Guid scriptId, CancellationToken ct)
    {
        var server = await GetServerOrThrow(serverId, ct);
        server.ScriptRequirements.Remove(scriptId);
        await _servers.UpdateAsync(server, ct);
    }

    private async Task<Server> GetServerOrThrow(Guid serverId, CancellationToken ct)
        => await _servers.GetAsync(serverId, ct)
            ?? throw new InfraBotException($"Сервер {serverId} не найден.");
}
