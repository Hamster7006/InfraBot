using InfraBot.Core.Interface.Repository;
using InfraBot.Entities;

namespace InfraBot.Infrastracture.Repository.Memory;

internal sealed class ServerRepository : IServerRepository
{
    private readonly List<Server> _servers = [];

    public Task AddAsync(Server server, CancellationToken ct)
    {
        _servers.Add(server);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Server server, CancellationToken ct)
    {
        var index = _servers.FindIndex(x => x.Id == server.Id);
        if (index < 0)
            throw new KeyNotFoundException($"Сервер {server.Id} не найден.");

        _servers[index] = server;
        return Task.CompletedTask;
    }

    public Task<Server?> GetAsync(Guid id, CancellationToken ct)
        => Task.FromResult(_servers.FirstOrDefault(x => x.Id == id));

    public Task<Server?> GetAsync(string name, CancellationToken ct)
        => Task.FromResult(_servers.FirstOrDefault(x =>
            string.Equals(x.ServerName, name, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<Server>> GetAllAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Server>>(_servers.ToList());

    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct)
        => Task.FromResult(_servers.Any(x =>
            string.Equals(x.ServerName, name, StringComparison.OrdinalIgnoreCase)));

    internal void LoadAll(IEnumerable<Server> servers, bool replace)
    {
        if (replace)
            _servers.Clear();

        _servers.AddRange(servers);
    }
}
