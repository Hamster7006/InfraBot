using InfraBot.Core.Exceptions;
using InfraBot.Core.Interface.Repository;
using InfraBot.Core.Interface.Services;
using InfraBot.Entities;

namespace InfraBot.Infrastracture.Services;

internal sealed class ScriptService : IScriptService
{
    private readonly IScriptRepository _scripts;

    public ScriptService(IScriptRepository scripts)
    {
        _scripts = scripts;
    }

    public async Task<Script> AddScriptAsync(Script script, CancellationToken ct)
    {
        if (await _scripts.ExistsByNameAsync(script.Name, ct))
            throw new InfraBotException($"Скрипт «{script.Name}» уже существует.");

        await _scripts.AddAsync(script, ct);
        return script;
    }

    public Task UpdateScriptAsync(Script script, CancellationToken ct)
        => _scripts.UpdateAsync(script, ct);

    public Task<Script?> GetScriptAsync(Guid id, CancellationToken ct)
        => _scripts.GetAsync(id, ct);

    public async Task<Script?> GetScriptByNameAsync(string name, CancellationToken ct)
    {
        var all = await _scripts.GetAllAsync(ct);
        return all.FirstOrDefault(x =>
            string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public Task<IReadOnlyList<Script>> GetAllScriptsAsync(CancellationToken ct)
        => _scripts.GetAllAsync(ct);
}
