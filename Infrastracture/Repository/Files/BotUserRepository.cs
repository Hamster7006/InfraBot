using System.Text.Json;
using System.Text.Json.Serialization;
using InfraBot.Core.Interface.Repository;
using InfraBot.Entities;

namespace InfraBot.Infrastracture.Repository.Files;

internal sealed class BotUserRepository : IBotUserRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public BotUserRepository(string botUsersFileName)
    {
        _filePath = botUsersFileName;
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    public Task AddUserAsync(BotUser user, CancellationToken ct)
        => AppendAsync(user, ct);

    public Task UpdateUserAsync(BotUser user, CancellationToken ct)
        => UpdateItemAsync(user, x => x.Id == user.Id, ct);

    public async Task<BotUser?> GetUserAsync(Guid id, CancellationToken ct)
    {
        var items = await ReadAllAsync(ct);
        return items.FirstOrDefault(x => x.Id == id);
    }

    public async Task<BotUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken ct)
    {
        var items = await ReadAllAsync(ct);
        return items.FirstOrDefault(x => x.TelegramId == telegramUserId);
    }

    public async Task<IReadOnlyList<BotUser>> GetAllAsync(CancellationToken ct)
        => await ReadAllAsync(ct);

    private async Task<List<BotUser>> ReadAllAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_filePath))
                return [];

            var lines = await File.ReadAllLinesAsync(_filePath, ct);
            var result = new List<BotUser>(lines.Length);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var item = JsonSerializer.Deserialize<BotUser>(line, JsonOptions);
                if (item is not null)
                    result.Add(item);
            }

            return result;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task AppendAsync(BotUser item, CancellationToken ct)
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

    private async Task UpdateItemAsync(BotUser item, Predicate<BotUser> match, CancellationToken ct)
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

    private async Task<List<BotUser>> ReadAllUnlockedAsync(CancellationToken ct)
    {
        if (!File.Exists(_filePath))
            return [];

        var lines = await File.ReadAllLinesAsync(_filePath, ct);
        var result = new List<BotUser>(lines.Length);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var item = JsonSerializer.Deserialize<BotUser>(line, JsonOptions);
            if (item is not null)
                result.Add(item);
        }

        return result;
    }

    private async Task WriteAllUnlockedAsync(List<BotUser> items, CancellationToken ct)
    {
        await using var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var writer = new StreamWriter(stream);
        foreach (var entry in items)
            await writer.WriteLineAsync(JsonSerializer.Serialize(entry, JsonOptions).AsMemory(), ct);
    }
}
