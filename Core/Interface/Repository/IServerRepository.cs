using InfraBot.Entities;

namespace InfraBot.Core.Interface.Repository;

internal interface IServerRepository
{
    Task AddAsync(Server server, CancellationToken ct);
    Task UpdateAsync(Server server, CancellationToken ct);
    Task<Server?> GetAsync(Guid id, CancellationToken ct);
}
