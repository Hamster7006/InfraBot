using InfraBot.Entities;

namespace InfraBot.Core.Interface.Repository;

internal interface IScriptRepository
{
    Task AddAsync(Script entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Script entity, CancellationToken cancellationToken = default);
    Task<Script?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
