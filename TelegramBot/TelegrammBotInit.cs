using InfraBot.Core.Interface.Repository;
using InfraBot.Core.Interface.Services;
using InfraBot.Entities;
using InfraBot.Enums;
using InfraBot.HelpData;
using InfraBot.Infrastracture.Services;
using InfraBot.TestData;
using System.Text.Json;

namespace InfraBot.TelegramBot;

internal class TelegrammBotInit
{
    private static readonly int data = 0; // позже убрать

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

        #region Запуск Telegram-бота
        await Task.CompletedTask;
        #endregion
    }

    public class ConfigData
    {
        public string Token { get; set; } = "0000000000:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    }
}


