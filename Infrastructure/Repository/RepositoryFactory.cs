using InfraBot.Core.Interface.Repository;
using InfraBot.Infrastructure.DataAccess;
using InfraBot.Infrastructure.Repository.DB;

namespace InfraBot.Infrastructure.Repository;

internal static class RepositoryFactory
{
    internal static (
        IBotUserRepository BotUserRepository,
        IServerRepository ServerRepository,
        IScriptRepository ScriptRepository,
        IJobRunRepository JobRunRepository,
        ISvcSamAccountRepository SvcRepository
    ) Create(string connectionString)
    {
        var dataContextFactory = new DataContextFactory(connectionString);

        return (
            new SqlBotUserRepository(dataContextFactory),
            new SqlServerRepository(dataContextFactory),
            new SqlScriptRepository(dataContextFactory),
            new SqlJobRunRepository(dataContextFactory),
            new SqlSvcSamAccountRepository(dataContextFactory));
    }
}
