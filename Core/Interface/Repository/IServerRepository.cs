using InfraBot.Entities;

namespace InfraBot.Core.Interface.Repository;

internal interface IServerRepository
{
    Task AddAsync(Server entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Server entity, CancellationToken cancellationToken = default);
    Task<Server?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
