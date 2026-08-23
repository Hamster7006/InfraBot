using InfraBot.Core.Exceptions;
using InfraBot.HelpData;
using InfraBot.Scenarios.Core;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace InfraBot.TelegramBot;

internal class TelegrammBotInit
{
    private const string ConfigFilePath = "config.json";

    public ConfigData Config { get; private set; } = null!;

    public async Task StartTelegrammBotInitAsync(CancellationToken ct = default)
    {
        var configDirectory = Path.GetDirectoryName(ConfigFilePath);
        if (!string.IsNullOrEmpty(configDirectory) && !Directory.Exists(configDirectory))
            Directory.CreateDirectory(configDirectory);

        if (!File.Exists(ConfigFilePath))
            File.Create(ConfigFilePath).Dispose();

        #region Получение конфигурации
        JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var defaultConfig = new ConfigData();
        ConfigData config;

        while (true)
        {
            if (File.Exists(ConfigFilePath) && new FileInfo(ConfigFilePath).Length > 0)
            {
                await using var stream = File.OpenRead(ConfigFilePath);
                config = JsonSerializer.Deserialize<ConfigData>(stream, jsonOptions) ?? new ConfigData();

                var tokenConfigured = !string.IsNullOrWhiteSpace(config.Token)
                    && config.Token != defaultConfig.Token;
                var connectionConfigured = !string.IsNullOrWhiteSpace(config.ConnectionString)
                    && config.ConnectionString != defaultConfig.ConnectionString;

                if (tokenConfigured && connectionConfigured)
                    break;
            }
            else
            {
                config = new ConfigData();
            }

            await File.WriteAllTextAsync(ConfigFilePath, JsonSerializer.Serialize(config, jsonOptions), ct);

            if (string.IsNullOrWhiteSpace(config.Token) || config.Token == defaultConfig.Token)
            {
                Console.WriteLine("Введите токен Telegram-бота:");
                config.Token = Console.ReadLine()?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(config.Token))
                    throw new InvalidOperationException("Токен Telegram-бота не указан.");
            }

            if (string.IsNullOrWhiteSpace(config.ConnectionString)
                || config.ConnectionString == defaultConfig.ConnectionString)
            {
                Console.WriteLine("Введите строку подключения к PostgreSQL:");
                config.ConnectionString = Console.ReadLine()?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(config.ConnectionString))
                    throw new InvalidOperationException("Строка подключения к БД не указана.");
            }

            await File.WriteAllTextAsync(ConfigFilePath, JsonSerializer.Serialize(config, jsonOptions), ct);
        }

        Config = config;
        #endregion

        #region Запуск Telegram-бота
        try
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            var botClient = new TelegramBotClient(Config.Token);
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new UpdateType[]
                {
                        UpdateType.Message,
                        UpdateType.CallbackQuery,
                        UpdateType.EditedMessage,
                },
                DropPendingUpdates = true
            };

            var commands = new List<BotCommand> { };
            foreach (var key in ConstantData.CommandsDictionary.Keys)
                commands.Add(
                        new BotCommand
                        {
                            Command = $"{key.Replace("/", "")}",
                            Description = $"{ConstantData.CommandsDictionary[key].Description}"
                        }
                    );
            await botClient.SetMyCommands(commands);

            IEnumerable<IScenario> scenarios = new List<IScenario>();
            var scenarioContextRepository = new InMemoryScenarioContextRepository();

            var handler = new UpdateHandler(
                botClient,
                Config.ConnectionString,
                scenarios,
                scenarioContextRepository,
                cancellationTokenSource.Token);

            botClient.StartReceiving(handler, receiverOptions, cancellationTokenSource.Token);

            var me = await botClient.GetMe();
            Console.WriteLine($"{me.FirstName} запущен!");
            Console.WriteLine("Нажмите клавишу A для выхода.");
            await Task.Run(() =>
            {
                while (true)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.A)
                    {
                        cancellationTokenSource.Cancel();
                        Console.WriteLine("Bot stopping...");
                        break;
                    }

                    Console.WriteLine($"Id телеграм бота: {me.Id}.");
                }
            });

            await Task.Delay(-1);
        }
        catch (InfraBotException ex)
        {
            Console.WriteLine("Произошла непредвиденная ошибка: ");
            Console.WriteLine($"Type of exception: {ex.GetType()}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            Console.WriteLine($"InnerException: {ex.InnerException}");
        }
        await Task.CompletedTask;
        #endregion
    }

    public class ConfigData
    {
        public string Token { get; set; } = "0000000000:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

        public string ConnectionString { get; set; } =
            "Host=localhost;Port=5432;Database=infrabot;Username=postgres;Password=changeme";
    }
}
