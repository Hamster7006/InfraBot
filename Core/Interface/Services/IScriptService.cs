using InfraBot.Entities;

namespace InfraBot.Core.Interface.Services;

public interface IScriptService
{
    Task<Script> AddScriptAsync(Script script, CancellationToken ct);
    Task UpdateScriptAsync(Script script, CancellationToken ct);
    Task<Script?> GetScriptAsync(Guid id, CancellationToken ct);
    Task<Script?> GetScriptByNameAsync(string name, CancellationToken ct);
    Task<IReadOnlyList<Script>> GetAllScriptsAsync(CancellationToken ct);
}
