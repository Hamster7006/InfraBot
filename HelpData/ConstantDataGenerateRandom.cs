using InfraBot.Core.Interface.Repository;
using InfraBot.Core.Interface.Services;
using InfraBot.Enums;
using InfraBot.Infrastracture.Services;
using InfraBot.TestData;

namespace InfraBot.HelpData;

internal class ConstantDataGenerateRandom
{
    internal (
        IBotUserRepository BotUserRepository,
        IServerRepository ServerRepository,
        IScriptRepository ScriptRepository,
        IJobRunRepository JobRunRepository,
        ISvcSamAccountRepository SvcRepository
    ) SwitchMemory(int data)
    {
        IBotUserRepository botUserRepository = null!;
        IServerRepository serverRepository = null!;
        IScriptRepository scriptRepository = null!;
        IJobRunRepository jobRunRepository = null!;
        ISvcSamAccountRepository svcRepository = null!;
        switch (data)
        {
            case 0:
            {
                botUserRepository = new Infrastracture.Repository.Memory.BotUserRepository();
                serverRepository = new Infrastracture.Repository.Memory.ServerRepository();
                scriptRepository = new Infrastracture.Repository.Memory.ScriptRepository();
                jobRunRepository = new Infrastracture.Repository.Memory.JobRunRepository();
                svcRepository = new Infrastracture.Repository.Memory.SvcSamAccountRepository();
                break;
            }
            case 1:
            {
                botUserRepository = new Infrastracture.Repository.Files.BotUserRepository(RepositoryPaths.BotUsers);
                serverRepository = new Infrastracture.Repository.Files.ServerRepository(RepositoryPaths.Servers);
                scriptRepository = new Infrastracture.Repository.Files.ScriptRepository(RepositoryPaths.ScriptsFolder);
                jobRunRepository = new Infrastracture.Repository.Files.JobRunRepository(RepositoryPaths.JobRunsFolder);
                svcRepository = new Infrastracture.Repository.Files.SvcSamAccountRepository(RepositoryPaths.SvcSamAccounts);
                break;
            }
            case 2: // бд
                botUserRepository = null!;
                serverRepository = null!;
                scriptRepository = null!;
                jobRunRepository = null!;
                svcRepository = null!;
                break;
        }

        return (botUserRepository, serverRepository, scriptRepository, jobRunRepository, svcRepository);
    }

    internal async Task<IBotUserService> GenerateTempData(
        int data,
        IBotUserService botUsersService,
        IBotUserRepository botUserRepository,
        IServerRepository serverRepository,
        IScriptRepository scriptRepository,
        IJobRunRepository jobRunRepository,
        ISvcSamAccountRepository svcSamAccountRepository,
        CancellationToken ct)
    {
        var testDataStorage = data switch
        {
            0 => TestDataStorageKind.Memory,
            1 => TestDataStorageKind.Files,
            _ => TestDataStorageKind.Memory
        };

        var testDataManager = new TestDataManager(new TestDataLoadOptions
        {
            Storage = testDataStorage,
            DataRootPath = "Data",
            ClearExisting = true,
            BotUserRepository = botUserRepository,
            ServerRepository = serverRepository,
            ScriptRepository = scriptRepository,
            JobRunRepository = jobRunRepository,
            SvcSamAccountRepository = svcSamAccountRepository
        });

        await testDataManager.GenerateAndLoadAsync(ct);

        var owner = await botUsersService.RegisterUserAsync(578566515, "Mad163Hamster", ct);
        if (owner.Status != UserStatus.Admin)
            await botUsersService.SetUserStatusAsync(owner.Id, UserStatus.Admin, ct);

        return botUsersService;
    }
}
