using System.Text.Json;
using System.Text.Json.Serialization;
using InfraBot.Core.Interface.Repository;
using InfraBot.Entities;

namespace InfraBot.Infrastracture.Repository.Files;

internal sealed class JobRunRepository : IJobRunRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _folderPath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JobRunRepository(string jobRunsFolder)
    {
        _folderPath = jobRunsFolder;
        Directory.CreateDirectory(_folderPath);
    }

    public async Task<JobRun> CreateAsync(BotUser user, Script script, Server server, CancellationToken ct)
    {
        var jobRun = new JobRun(script.Id, server.Id, user.Id);
        await AddItemAsync(jobRun, ct);
        return jobRun;
    }

    public Task UpdateAsync(JobRun jobRun, CancellationToken ct)
        => UpdateItemAsync(jobRun, x => x.Id == jobRun.Id, ct);

    public async Task<JobRun?> GetAsync(Guid id, CancellationToken ct)
    {
        var items = await ReadAllAsync(ct);
        return items.FirstOrDefault(x => x.Id == id);
    }

    public async Task<IReadOnlyList<JobRun>> GetByIdsUserOrServer(Guid? userId, Guid? serverId, CancellationToken ct)
    {
        var items = await ReadAllAsync(ct);
        return items
            .Where(x =>
                (userId.HasValue && x.InitiatedById == userId.Value) ||
                (serverId.HasValue && x.ServerId == serverId.Value))
            .ToList();
    }

    public async Task<IReadOnlyList<JobRun>> GetByUserIdAndServer(Guid userId, Guid? serverId, CancellationToken ct)
    {
        var items = await ReadAllAsync(ct);
        return items
            .Where(x =>
                x.InitiatedById == userId &&
                (!serverId.HasValue || x.ServerId == serverId.Value))
            .ToList();
    }

    public async Task<IReadOnlyList<JobRun>> GetByServerIdAndUser(Guid serverId, Guid? userId, CancellationToken ct)
    {
        var items = await ReadAllAsync(ct);
        return items
            .Where(x =>
                x.ServerId == serverId &&
                (!userId.HasValue || x.InitiatedById == userId.Value))
            .ToList();
    }

    private async Task<List<JobRun>> ReadAllAsync(CancellationToken ct)
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

    private async Task AddItemAsync(JobRun item, CancellationToken ct)
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

    private async Task UpdateItemAsync(JobRun item, Predicate<JobRun> match, CancellationToken ct)
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

    private async Task<List<JobRun>> ReadAllUnlockedAsync(CancellationToken ct)
    {
        if (!Directory.Exists(_folderPath))
            return [];

        var result = new List<JobRun>();
        foreach (var path in Directory.EnumerateFiles(_folderPath, "*.json"))
        {
            await using var stream = File.OpenRead(path);
            var item = await JsonSerializer.DeserializeAsync<JobRun>(stream, JsonOptions, ct);
            if (item is not null)
                result.Add(item);
        }

        return result;
    }

    private async Task WriteItemUnlockedAsync(JobRun item, CancellationToken ct)
    {
        var path = GetFilePath(item.Id);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, item, JsonOptions, ct);
    }
}
