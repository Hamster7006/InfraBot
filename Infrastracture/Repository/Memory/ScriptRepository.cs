using InfraBot.Core.Interface.Repository;
using InfraBot.Entities;

namespace InfraBot.Infrastracture.Repository.Memory;

internal sealed class ScriptRepository : IScriptRepository
{
    private readonly List<Script> _scripts = [];

    public Task AddAsync(Script script, CancellationToken ct)
    {
        _scripts.Add(script);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Script script, CancellationToken ct)
    {
        var index = _scripts.FindIndex(x => x.Id == script.Id);
        if (index < 0)
            throw new KeyNotFoundException($"Скрипт {script.Id} не найден.");

        _scripts[index] = script;
        return Task.CompletedTask;
    }

    public Task<Script?> GetAsync(Guid id, CancellationToken ct)
        => Task.FromResult(_scripts.FirstOrDefault(x => x.Id == id));

    public Task<IReadOnlyList<Script>> GetAllAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Script>>(_scripts.ToList());

    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct)
        => Task.FromResult(_scripts.Any(x =>
            string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)));

    internal void LoadAll(IEnumerable<Script> scripts, bool replace)
    {
        if (replace)
            _scripts.Clear();

        _scripts.AddRange(scripts);
    }
}
