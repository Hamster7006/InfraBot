using InfraBot.Entities;

namespace InfraBot.Core.Interface.Repository;

internal interface IServerScriptsRepository
{
    Task AddAsync(ServerScripts entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(ServerScripts entity, CancellationToken cancellationToken = default);
    Task<ServerScripts?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
