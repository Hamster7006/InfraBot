using InfraBot.Core.Interface.Repository;
using InfraBot.Enums;
using InfraBot.TelegramBot;
using InfraBot.TestData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfraBot.HelpData
{
    internal class ConstantDataGenerateRandom
    {
        IBotUserRepository _botUserRepository;
        IServerRepository _serverRepository;
        IScriptRepository _scriptRepository;
        IJobRunRepository _jobRunRepository;
        ISvcSamAccountRepository _svcRepository;
        
        internal (
            IBotUserRepository _botUserRepository,
            IServerRepository _serverRepository,
            IScriptRepository _scriptRepository,
            IJobRunRepository _jobRunRepository,
            ISvcSamAccountRepository _svcRepository) SwitchMemory(int data)
        {
            switch (data)
            {
                case 0:
                    {
                        _botUserRepository = new Infrastracture.Repository.Memory.BotUserRepository();
                        _serverRepository = new Infrastracture.Repository.Memory.ServerRepository();
                        _scriptRepository = new Infrastracture.Repository.Memory.ScriptRepository();
                        _jobRunRepository = new Infrastracture.Repository.Memory.JobRunRepository();
                        _svcRepository = new Infrastracture.Repository.Memory.SvcSamAccountRepository();
                        break;
                    }
                case 1:
                    {
                        _botUserRepository = new Infrastracture.Repository.Files.BotUserRepository(RepositoryPaths.BotUsers);
                        _serverRepository = new Infrastracture.Repository.Files.ServerRepository(RepositoryPaths.Servers);
                        _scriptRepository = new Infrastracture.Repository.Files.ScriptRepository(RepositoryPaths.ScriptsFolder);
                        _jobRunRepository = new Infrastracture.Repository.Files.JobRunRepository(RepositoryPaths.JobRunsFolder);
                        _svcRepository = new Infrastracture.Repository.Files.SvcSamAccountRepository(RepositoryPaths.SvcSamAccounts);
                        break;
                    }
                case 2: // бд
                    {
                        break;
                    }
                default: { break; }
            }
            return (_botUserRepository, _serverRepository, _scriptRepository, _jobRunRepository, _svcRepository);
        }
        internal async Task GenerateTempData(int data, CancellationToken ct)
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
                ClearExisting = true
            });
            await testDataManager.GenerateAndLoadAsync(ct);
            var owner = await _botUsersService.RegisterUserAsync(578566515, "Mad163Hamster", ct);
            if (owner.Status != UserStatus.Admin)
                await _botUsersService.SetUserStatusAsync(owner.Id, UserStatus.Admin, ct);
        }
    }
}
