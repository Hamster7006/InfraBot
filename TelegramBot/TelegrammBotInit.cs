using InfraBot.Core.Interface.Repository;
using InfraBot.Core.Interface.Services;
using InfraBot.Entities;
using InfraBot.Enums;
using InfraBot.Infrastracture.Services;
using InfraBot.TestData;
using System;
using System.Text.Json;

namespace InfraBot.TelegramBot;

internal class TelegrammBotInit
{
    private static readonly int data = 0;
    

    public IBotUserService BotUsers { get; private set; } = null!;
    public IServerService Servers { get; private set; } = null!;
    public IScriptService Scripts { get; private set; } = null!;
    public IJobRunService JobRuns { get; private set; } = null!;
    public ISvcSamAccountService SvcAccounts { get; private set; } = null!;
    public ConfigData Config { get; private set; } = null!;

    public async Task StartTelegrammBotInitAsync(CancellationToken ct = default)
    {
        var pathInfo = new Dictionary<string, (string Path, bool IsFile)>
        {
            ["config"] = ("config.json", true),
            ["botUsers"] = (RepositoryPaths.BotUsers, true),
            ["servers"] = (RepositoryPaths.Servers, true),
            ["scripts"] = (RepositoryPaths.ScriptsFolder, false),
            ["jobRuns"] = (RepositoryPaths.JobRunsFolder, false),
            ["svcSamAccounts"] = (RepositoryPaths.SvcSamAccounts, true),
        };

        #region Проверка существование папок и файлов
        foreach (var key in pathInfo.Keys)
        {
            var obj = pathInfo[key];
            if (obj.IsFile)
            {
                var directory = Path.GetDirectoryName(obj.Path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                if (!File.Exists(obj.Path))
                    File.Create(obj.Path).Dispose();
            }
            else if (!Directory.Exists(obj.Path))
            {
                Directory.CreateDirectory(obj.Path);
            }
        }
        #endregion

        #region Получение конфигурации
        JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var configFilePath = pathInfo["config"].Path;
        var defaultToken = new ConfigData().Token;
        ConfigData config;

        while (true)
        {
            if (File.Exists(configFilePath) && new FileInfo(configFilePath).Length > 0)
            {
                await using var stream = File.OpenRead(configFilePath);
                config = JsonSerializer.Deserialize<ConfigData>(stream, JsonOptions) ?? new ConfigData();

                if (!string.IsNullOrWhiteSpace(config.Token) && config.Token != defaultToken)
                    break;
            }

            config = new ConfigData();
            await File.WriteAllTextAsync(configFilePath, JsonSerializer.Serialize(config, JsonOptions), ct);

            Console.WriteLine("Введите токен Telegram-бота:");
            config.Token = Console.ReadLine()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(config.Token))
                throw new InvalidOperationException("Токен Telegram-бота не указан.");

            await File.WriteAllTextAsync(configFilePath, JsonSerializer.Serialize(config, JsonOptions), ct);
        }

        Config = config;
        #endregion

        #region Инициализация хранилища и репозиториев
        // Переключение хранилища: 0 — Memory, 1 — Files, 2 — БД
        IBotUserRepository botUserRepo = null!;
        IServerRepository serverRepo = null!;
        IScriptRepository scriptRepo = null!;
        IJobRunRepository jobRunRepo = null!;
        ISvcSamAccountRepository svcRepo = null!;
        MemoryRepositorySet? memoryRepositories = null;
        switch (data)
        {
            case 0: {
                    var memBotUsers = new Infrastracture.Repository.Memory.BotUserRepository();
                    var memServers = new Infrastracture.Repository.Memory.ServerRepository();
                    var memScripts = new Infrastracture.Repository.Memory.ScriptRepository();
                    var memJobRuns = new Infrastracture.Repository.Memory.JobRunRepository();
                    var memSvc = new Infrastracture.Repository.Memory.SvcSamAccountRepository();
                    botUserRepo = memBotUsers;
                    serverRepo = memServers;
                    scriptRepo = memScripts;
                    jobRunRepo = memJobRuns;
                    svcRepo = memSvc;
                    memoryRepositories = new MemoryRepositorySet
                    {
                        BotUsers = memBotUsers,
                        Servers = memServers,
                        Scripts = memScripts,
                        JobRuns = memJobRuns,
                        SvcSamAccounts = memSvc
                    };
                    break;
                }
            case 1:
                {
                     botUserRepo = new Infrastracture.Repository.Files.BotUserRepository(RepositoryPaths.BotUsers);
                     serverRepo = new Infrastracture.Repository.Files.ServerRepository(RepositoryPaths.Servers);
                     scriptRepo = new Infrastracture.Repository.Files.ScriptRepository(RepositoryPaths.ScriptsFolder);
                     jobRunRepo = new Infrastracture.Repository.Files.JobRunRepository(RepositoryPaths.JobRunsFolder);
                     svcRepo = new Infrastracture.Repository.Files.SvcSamAccountRepository(RepositoryPaths.SvcSamAccounts);
                    break;
                }
            case 2: // бд
                {
                    break;
                }
            default: { break; }
        }



        #endregion

        #region Регистрация сервисов
        BotUsers = new BotUserService(botUserRepo);
        Servers = new ServerService(serverRepo);
        Scripts = new ScriptService(scriptRepo);
        JobRuns = new JobRunService(jobRunRepo, serverRepo, scriptRepo);
        SvcAccounts = new SvcSamAccountService(svcRepo);
        #endregion

        #region Генерация тестовых данных
        Console.WriteLine("Генерация и загрузка тестовых данных...");
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
            MemoryRepositories = memoryRepositories
        });
        await testDataManager.GenerateAndLoadAsync(ct);
        var owner = await BotUsers.RegisterUserAsync(578566515, "Mad163Hamster", ct);
        if (owner.Status != UserStatus.Admin)
            await BotUsers.SetUserStatusAsync(owner.Id, UserStatus.Admin, ct);
        owner = (await BotUsers.GetUserByIdAsync(owner.Id, ct))!;
        Console.WriteLine($"Owner user: {owner.Username} ({owner.TelegramId}), role: {owner.Status}");
        #endregion

        #region Запуск Telegram-бота
        await Task.CompletedTask;
        #endregion
    }

    public class ConfigData
    {
        public string Token { get; set; } = "0000000000:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    }
}
internal static class RepositoryPaths
{
    internal const string BotUsers = "Data\\bot_users.json";
    internal const string Servers = "Data\\servers.json";
    internal const string ScriptsFolder = "Data\\Scripts";
    internal const string JobRunsFolder = "Data\\Jobs";
    internal const string SvcSamAccounts = "Data\\svcSamAccounts.json";
}