using System.Collections.Concurrent;

namespace InfraBot.Scenarios.Core;

internal sealed class InMemoryScenarioContextRepository : IScenarioContextRepository
{
    private readonly ConcurrentDictionary<long, ScenarioContext> _contexts = new();

    public Task<ScenarioContext?> GetContext(long userId, CancellationToken ct)
        => Task.FromResult(_contexts.TryGetValue(userId, out var context) ? context : null);

    public Task SetContext(long userId, ScenarioContext context, CancellationToken ct)
    {
        _contexts[userId] = context;
        return Task.CompletedTask;
    }

    public Task ResetContext(long userId, CancellationToken ct)
    {
        _contexts.TryRemove(userId, out _);
        return Task.CompletedTask;
    }
}
