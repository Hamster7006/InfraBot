using InfraBot.Core.Interface.Repository;
using InfraBot.Infrastracture.DataAccess;
using InfraBot.Infrastracture.Repository.DB;

namespace InfraBot.HelpData;

internal class ConstantDataGenerateRandom
{
    internal (
        IBotUserRepository BotUserRepository,
        IServerRepository ServerRepository,
        IScriptRepository ScriptRepository,
        IJobRunRepository JobRunRepository,
        ISvcSamAccountRepository SvcRepository
    ) SwitchMemory(int data, string? connectionString = null)
    {
        IBotUserRepository botUserRepository = null!;
        IServerRepository serverRepository = null!;
        IScriptRepository scriptRepository = null!;
        IJobRunRepository jobRunRepository = null!;
        ISvcSamAccountRepository svcRepository = null!;
        switch (data)
        {
            case 1:
            {
                botUserRepository = new Infrastracture.Repository.Files.BotUserRepository(RepositoryPaths.BotUsers);
                serverRepository = new Infrastracture.Repository.Files.ServerRepository(RepositoryPaths.Servers);
                scriptRepository = new Infrastracture.Repository.Files.ScriptRepository(RepositoryPaths.ScriptsFolder);
                jobRunRepository = new Infrastracture.Repository.Files.JobRunRepository(RepositoryPaths.JobRunsFolder);
                svcRepository = new Infrastracture.Repository.Files.SvcSamAccountRepository(RepositoryPaths.SvcSamAccounts);
                break;
            }
            case 2:
            {
                var dataContextFactory = new DataContextFactory(connectionString!);
                dataContextFactory.CreateDataContext();
                botUserRepository = new SqlBotUserRepository(dataContextFactory);
                serverRepository = new SqlServerRepository(dataContextFactory);
                scriptRepository = new SqlScriptRepository(dataContextFactory);
                jobRunRepository = new SqlJobRunRepository(dataContextFactory);
                svcRepository = new SqlSvcSamAccountRepository(dataContextFactory);
                break;
            }
        }

        return (botUserRepository, serverRepository, scriptRepository, jobRunRepository, svcRepository);
    }
}
