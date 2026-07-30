using System.Text.Json;
using System.Text.Json.Serialization;
using InfraBot.Core.Interface.Repository;
using InfraBot.Entities;

namespace InfraBot.Infrastracture.Repository.Files;

internal sealed class ScriptRepository : IScriptRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _folderPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ScriptRepository(string scriptsFolder)
    {
        _folderPath = scriptsFolder;
        Directory.CreateDirectory(_folderPath);
    }

    public Task AddAsync(Script script, CancellationToken ct)
        => AddItemAsync(script, ct);

    public Task UpdateAsync(Script script, CancellationToken ct)
        => UpdateItemAsync(script, x => x.Id == script.Id, ct);

    public async Task<Script?> GetAsync(Guid id, CancellationToken ct)
    {
        var items = await ReadAllAsync(ct);
        return items.FirstOrDefault(x => x.Id == id);
    }

    public async Task<IReadOnlyList<Script>> GetAllAsync(CancellationToken ct)
        => await ReadAllAsync(ct);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct)
    {
        var items = await ReadAllAsync(ct);
        return items.Any(x =>
            string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<List<Script>> ReadAllAsync(CancellationToken ct)
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

    private async Task AddItemAsync(Script item, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var path = GetFilePath(item.Id);
            if (File.Exists(path))
                throw new InvalidOperationException($"Файл уже существует: {path}");

            await WriteItemUnlockedAsync(item, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task UpdateItemAsync(Script item, Predicate<Script> match, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var items = await ReadAllUnlockedAsync(ct);
            if (!items.Exists(match))
                throw new KeyNotFoundException("Запись для обновления не найдена.");

            await WriteItemUnlockedAsync(item, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private string GetFilePath(Guid id) => Path.Combine(_folderPath, $"{id}.json");

    private async Task<List<Script>> ReadAllUnlockedAsync(CancellationToken ct)
    {
        if (!Directory.Exists(_folderPath))
            return [];

        var result = new List<Script>();
        foreach (var path in Directory.EnumerateFiles(_folderPath, "*.json"))
        {
            await using var stream = File.OpenRead(path);
            var item = await JsonSerializer.DeserializeAsync<Script>(stream, JsonOptions, ct);
            if (item is not null)
                result.Add(item);
        }

        return result;
    }

    private async Task WriteItemUnlockedAsync(Script item, CancellationToken ct)
    {
        var path = GetFilePath(item.Id);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, item, JsonOptions, ct);
    }
}
