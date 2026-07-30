using InfraBot.Entities;

namespace InfraBot.Core.Interface.Repository;

internal interface IServerScriptsRepository
{
    Task AddAsync(ServerScripts serverScripts, CancellationToken ct);
    Task UpdateAsync(ServerScripts serverScripts, CancellationToken ct);
    Task<ServerScripts?> GetAsync(Guid id, CancellationToken ct);
}
