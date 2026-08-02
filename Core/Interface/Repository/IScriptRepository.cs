using InfraBot.Entities;

namespace InfraBot.Core.Interface.Repository;

internal interface IScriptRepository
{
    Task AddAsync(Script script, CancellationToken ct);
    Task UpdateAsync(Script script, CancellationToken ct);
    Task<Script?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Script>> GetAllAsync(CancellationToken ct);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}
