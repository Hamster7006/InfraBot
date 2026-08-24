using InfraBot.Core.Exceptions;
using InfraBot.Core.Interface.Repository;
using InfraBot.Core.Interface.Services;
using InfraBot.Entities;
using InfraBot.Enums;

namespace InfraBot.Infrastructure.Services;

internal sealed class ServerService : IServerService
{
    private readonly IServerRepository _servers;

    public ServerService(IServerRepository servers)
    {
        _servers = servers;
    }

    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct)
        => _servers.ExistsByNameAsync(name, ct);

    public async Task<Server> AddServerAsync(Server server, CancellationToken ct)
    {
        if (await _servers.ExistsByNameAsync(server.ServerName, ct))
            throw new InfraBotException($"Сервер «{server.ServerName}» уже существует.");

        if (string.IsNullOrWhiteSpace(server.IpAddress))
            throw new InfraBotException("Для сервера необходим IP-адрес.");

        if (server.SvcSamAccountId == Guid.Empty)
            throw new InfraBotException("Для сервера необходима учётная запись WinRM.");

        await _servers.AddAsync(server, ct);
        return server;
    }

    public Task UpdateServerAsync(Server server, CancellationToken ct)
    {
        if (server.SvcSamAccountId == Guid.Empty)
            throw new InfraBotException("Для сервера необходима учётная запись WinRM.");

        return _servers.UpdateAsync(server, ct);
    }

    public Task<Server?> GetServerAsync(Guid id, CancellationToken ct)
        => _servers.GetAsync(id, ct);

    public async Task<IReadOnlyList<Server>> GetAccessibleServersAsync(BotUser user, CancellationToken ct)
    {
        var all = await _servers.GetAllAsync(ct);

        if (user.Status == UserStatus.Admin)
            return all;
        else
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

    public async Task AddScriptToServerAsync(Guid serverId, Guid scriptId, CancellationToken ct)
    {
        var server = await GetServerOrThrow(serverId, ct);
        if (!server.ScriptRequirements.Contains(scriptId))
            server.ScriptRequirements.Add(scriptId);

        await _servers.UpdateAsync(server, ct);
    }

    public async Task RemoveScriptFromServerAsync(Guid serverId, Guid scriptId, CancellationToken ct)
    {
        var server = await GetServerOrThrow(serverId, ct);
        server.ScriptRequirements.Remove(scriptId);
        await _servers.UpdateAsync(server, ct);
    }

    public async Task RemoveScriptFromAllServersAsync(Guid scriptId, CancellationToken ct)
    {
        var servers = await _servers.GetAllAsync(ct);
        foreach (var server in servers.Where(s => s.ScriptRequirements.Contains(scriptId)))
        {
            server.ScriptRequirements.Remove(scriptId);
            await _servers.UpdateAsync(server, ct);
        }
    }

    public async Task DeleteServerAsync(Guid serverId, CancellationToken ct)
    {
        await GetServerOrThrow(serverId, ct);
        await _servers.DeleteAsync(serverId, ct);
    }

    public async Task<IReadOnlyList<Server>> GetServersBySvcAccountAsync(Guid svcAccountId, CancellationToken ct)
    {
        var all = await _servers.GetAllAsync(ct);
        return all.Where(s => s.SvcSamAccountId == svcAccountId).ToList();
    }

    public async Task<IReadOnlyList<Server>> GetServersByScriptAsync(Guid scriptId, CancellationToken ct)
    {
        var all = await _servers.GetAllAsync(ct);
        return all.Where(s => s.ScriptRequirements.Contains(scriptId)).ToList();
    }

    private async Task<Server> GetServerOrThrow(Guid serverId, CancellationToken ct)
        => await _servers.GetAsync(serverId, ct)
            ?? throw new InfraBotException($"Сервер {serverId} не найден.");
}
