using InfraBot.Core.Interface.Repository;
using InfraBot.Infrastracture.DataAccess;
using InfraBot.Infrastracture.Repository.DB;

namespace InfraBot.Infrastracture.Repository;

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
        dataContextFactory.CreateDataContext();

        return (
            new SqlBotUserRepository(dataContextFactory),
            new SqlServerRepository(dataContextFactory),
            new SqlScriptRepository(dataContextFactory),
            new SqlJobRunRepository(dataContextFactory),
            new SqlSvcSamAccountRepository(dataContextFactory));
    }
}
