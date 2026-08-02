using System.Text.Json;
using InfraBot.Core.Interface.Repository;
using InfraBot.Entities;

namespace InfraBot.Infrastracture.Repository.Files;

internal sealed class ServerRepository : IServerRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ServerRepository(string serversFileName)
    {
        _filePath = serversFileName;
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    public Task AddAsync(Server server, CancellationToken ct)
        => AppendAsync(server, ct);

    public Task UpdateAsync(Server server, CancellationToken ct)
        => UpdateItemAsync(server, x => x.Id == server.Id, ct);

    public async Task<Server?> GetAsync(Guid id, CancellationToken ct)
    {
        var items = await ReadAllAsync(ct);
        return items.FirstOrDefault(x => x.Id == id);
    }

    public async Task<Server?> GetAsync(string name, CancellationToken ct)
    {
        var items = await ReadAllAsync(ct);
        return items.FirstOrDefault(x =>
            string.Equals(x.ServerName, name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<Server>> GetAllAsync(CancellationToken ct)
        => await ReadAllAsync(ct);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct)
    {
        var items = await ReadAllAsync(ct);
        return items.Any(x =>
            string.Equals(x.ServerName, name, StringComparison.OrdinalIgnoreCase));
    }

    public Task DeleteAsync(Guid id, CancellationToken ct)
        => DeleteItemAsync(x => x.Id == id, ct);

    private async Task<List<Server>> ReadAllAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            return await ReadAllUnlockedAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task AppendAsync(Server item, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var line = JsonSerializer.Serialize(item, JsonOptions);
            await File.AppendAllTextAsync(_filePath, line + Environment.NewLine, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task UpdateItemAsync(Server item, Predicate<Server> match, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var items = await ReadAllUnlockedAsync(ct);
            var index = items.FindIndex(match);
            if (index < 0)
                throw new KeyNotFoundException("Запись для обновления не найдена.");

            items[index] = item;
            await WriteAllUnlockedAsync(items, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task DeleteItemAsync(Predicate<Server> match, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var items = await ReadAllUnlockedAsync(ct);
            var index = items.FindIndex(match);
            if (index < 0)
                throw new KeyNotFoundException("Сервер не найден.");

            items.RemoveAt(index);
            await WriteAllUnlockedAsync(items, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<Server>> ReadAllUnlockedAsync(CancellationToken ct)
    {
        if (!File.Exists(_filePath))
            return [];

        var lines = await File.ReadAllLinesAsync(_filePath, ct);
        var result = new List<Server>(lines.Length);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var item = JsonSerializer.Deserialize<Server>(line, JsonOptions);
            if (item is not null)
                result.Add(item);
        }

        return result;
    }

    private async Task WriteAllUnlockedAsync(List<Server> items, CancellationToken ct)
    {
        await using var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var writer = new StreamWriter(stream);
        foreach (var entry in items)
            await writer.WriteLineAsync(JsonSerializer.Serialize(entry, JsonOptions).AsMemory(), ct);
    }
}
