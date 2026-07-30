using System.Text.Json;
using System.Text.Json.Serialization;
using InfraBot.Core.Interface.Repository;
using InfraBot.Entities;

namespace InfraBot.Infrastracture.Repository.Files;

internal sealed class SvcSamAccountRepository : ISvcSamAccountRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SvcSamAccountRepository(string svcSamAccountsFileName)
    {
        _filePath = svcSamAccountsFileName;
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    public Task AddAsync(SvcSamAccount svc, CancellationToken ct)
        => AppendAsync(svc, ct);

    public Task UpdateAsync(SvcSamAccount svc, CancellationToken ct)
        => UpdateItemAsync(svc, x => x.Id == svc.Id, ct);

    public async Task<SvcSamAccount?> GetAsync(Guid id, CancellationToken ct)
    {
        var items = await ReadAllAsync(ct);
        return items.FirstOrDefault(x => x.Id == id);
    }

    private async Task<List<SvcSamAccount>> ReadAllAsync(CancellationToken ct)
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

    private async Task AppendAsync(SvcSamAccount item, CancellationToken ct)
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

    private async Task UpdateItemAsync(SvcSamAccount item, Predicate<SvcSamAccount> match, CancellationToken ct)
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

    private async Task<List<SvcSamAccount>> ReadAllUnlockedAsync(CancellationToken ct)
    {
        if (!File.Exists(_filePath))
            return [];

        var lines = await File.ReadAllLinesAsync(_filePath, ct);
        var result = new List<SvcSamAccount>(lines.Length);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var item = JsonSerializer.Deserialize<SvcSamAccount>(line, JsonOptions);
            if (item is not null)
                result.Add(item);
        }

        return result;
    }

    private async Task WriteAllUnlockedAsync(List<SvcSamAccount> items, CancellationToken ct)
    {
        await using var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var writer = new StreamWriter(stream);
        foreach (var entry in items)
            await writer.WriteLineAsync(JsonSerializer.Serialize(entry, JsonOptions).AsMemory(), ct);
    }
}
