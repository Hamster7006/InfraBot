using InfraBot.Core.Exceptions;
using InfraBot.Core.Interface.Repository;
using InfraBot.Core.Interface.Services;
using InfraBot.Entities;
using InfraBot.Enums;
using InfraBot.HelpData;
using InfraBot.Infrastracture.Services;
using InfraBot.Scenarios.Core;
using InfraBot.TestData;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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
        try
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            var botClient = new TelegramBotClient(Config.Token);
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new UpdateType[]
                {
                        UpdateType.Message, //сообщение
                        //UpdateType.InlineQuery, // Запрос?
                        //UpdateType.ChosenInlineResult, // Запрос?
                        UpdateType.CallbackQuery, // клавиатура в сообщении
                        UpdateType.EditedMessage, // отредактированное сообщение
                                                  //UpdateType.ChannelPost, // пост в канале
                                                  //UpdateType.EditedChannelPost, // пост в канале отредактированный
                                                  //UpdateType.ShippingQuery, //??
                                                  //UpdateType.PreCheckoutQuery,//??
                                                  //UpdateType.Poll,
                                                  //UpdateType.PollAnswer,
                                                  //UpdateType.MyChatMember,
                                                  //UpdateType.ChatMember,
                                                  //UpdateType.ChatJoinRequest,
                                                  //UpdateType.MessageReaction, // реакция на соообщение
                                                  //UpdateType.MessageReactionCount, // Счетчик реакций на сообщение
                                                  //UpdateType.ChatBoost, // буст канала
                                                  //UpdateType.RemovedChatBoost, // Отключение буста
                                                  //UpdateType.BusinessConnection,//??
                                                  //UpdateType.BusinessMessage,//??
                                                  //UpdateType.EditedBusinessMessage,//??
                                                  //UpdateType.DeletedBusinessMessages,//??
                                                  //UpdateType.PurchasedPaidMedia,//??
                                                  //UpdateType.ManagedBot,//??
                                                  //UpdateType.GuestMessage,//??
                }

                ,
                DropPendingUpdates = true
            };


            // Создаем список команд
            var commands = new List<BotCommand>{};
            foreach (var key in ConstantData.CommandsDictionary.Keys)
                commands.Add(
                        new BotCommand { 
                                Command = $"{key.Replace("/", "")}", 
                                Description = $"{ConstantData.CommandsDictionary[key].Description}" 
                        }
                    );
            // Устанавливаем команды
            await botClient.SetMyCommands(commands);
            var handler = new UpdateHandler(botClient, data, ct);
            await handler.LoadTestDataAsync(data, ct);
            botClient.StartReceiving(handler, receiverOptions, cancellationTokenSource.Token);

            var me = await botClient.GetMe();
            Console.WriteLine($"{me.FirstName} запущен!");
            Console.WriteLine($"Нажмите клавишу A для выхода.");
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
                    else
                    {
                        Console.WriteLine($"Id телеграм бота: {me.Id}.");
                    }
                }
            });

            await Task.Delay(-1); // Устанавливаем бесконечную задержку.
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
    }
}


