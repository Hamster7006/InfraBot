using InfraBot.Core.Interface.Repository;
using InfraBot.Entities;
using InfraBot.Infrastructure.DataAccess;
using LinqToDB;
using LinqToDB.Async;

namespace InfraBot.Infrastructure.Repository.DB;

internal sealed class SqlBotUserRepository : IBotUserRepository
{
    private readonly IDataContextFactory<InfraDataContext> _factory;

    public SqlBotUserRepository(IDataContextFactory<InfraDataContext> factory)
    {
        _factory = factory;
    }

    public async Task AddUserAsync(BotUser user, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        await db.InsertAsync(ModelMapper.MapToModel(user), token: ct);
    }

    public async Task UpdateUserAsync(BotUser user, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        await db.UpdateAsync(ModelMapper.MapToModel(user), token: ct);
    }

    public async Task<BotUser?> GetUserAsync(Guid id, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        var model = await db.BotUsers.FirstOrDefaultAsync(u => u.Id == id, ct);
        return model is null ? null : ModelMapper.MapFromModel(model);
    }

    public async Task<BotUser?> GetUserByTelegramUserIdAsync(long telegramUserId, CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        var model = await db.BotUsers.FirstOrDefaultAsync(u => u.TelegramId == telegramUserId, ct);
        return model is null ? null : ModelMapper.MapFromModel(model);
    }

    public async Task<IReadOnlyList<BotUser>> GetAllAsync(CancellationToken ct)
    {
        using var db = _factory.CreateDataContext();
        var models = await db.BotUsers.ToListAsync(ct);
        return models.Select(ModelMapper.MapFromModel).ToList();
    }
}
