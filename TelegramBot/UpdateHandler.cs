using InfraBot.Core.Interface.Repository;
using InfraBot.Core.Interface.Services;
using InfraBot.Entities;
using InfraBot.Enums;
using InfraBot.HelpData;
using InfraBot.Infrastracture.Services;
using InfraBot.TestData;
using System;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static InfraBot.TelegramBot.TelegrammBotInit;
using static LinqToDB.Common.Configuration;
using static System.Net.Mime.MediaTypeNames;

namespace InfraBot.TelegramBot;

internal class UpdateHandler : IUpdateHandler
{
    ITelegramBotClient _telegramBotClient;
    ReplyKeyboardMarkup _replyKeyboardMarkup;
    BotUser? _userData;

    IBotUserRepository _botUserRepository;
    IServerRepository _serverRepository;
    IScriptRepository _scriptRepository;
    IJobRunRepository _jobRunRepository;
    ISvcSamAccountRepository _svcRepository;

    IBotUserService _botUsersService;
    IServerService _serversService;
    IScriptService _scriptsService;
    IJobRunService _jobRunsService;
    ISvcSamAccountService _svcAccountsService;


    public UpdateHandler(ITelegramBotClient telegramBotClient, int data)
    {
        //_botUsersService = botUsers;
        _telegramBotClient = telegramBotClient;

        var constantDataGenerateRandom = new ConstantDataGenerateRandom();
        (   IBotUserRepository botUserRepository, 
            IServerRepository serverRepository, 
            IScriptRepository scriptRepository, 
            IJobRunRepository jobRunRepository, 
            ISvcSamAccountRepository svcRepository) = constantDataGenerateRandom.SwitchMemory(data);
        _serverRepository = serverRepository;
        _scriptRepository = scriptRepository;
        _botUserRepository = botUserRepository;
        _jobRunRepository = jobRunRepository;
        _svcRepository = svcRepository;


        _botUsersService = new BotUserService(_botUserRepository);
        _serversService = new ServerService(_serverRepository);
        _scriptsService = new ScriptService(_scriptRepository);
        _jobRunsService = new JobRunService(_jobRunRepository, _serverRepository, _scriptRepository);
        _svcAccountsService = new SvcSamAccountService(_svcRepository);
    }

    public Task HandleErrorAsync(
        ITelegramBotClient botUsers,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Telegram error ({source}): {exception.Message}");
        return Task.CompletedTask;
    }

    public async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        await (update switch
        {
            { Message: { } message } => OnMessage(update, cancellationToken),
            { CallbackQuery: { } callbackQuery } => OnCallbackQuery(update, callbackQuery, cancellationToken),
            _ => OnUnknown(update)
        });
    }

    public Task OnCallbackQuery(Update update, CallbackQuery callbackQuery, CancellationToken ct)
    {
        //отправить запрос и текущий статус пользователя
        return Task.CompletedTask;
    }

    public Task OnUnknown(Update update)
    {
        //отправить запрос и текущий статус пользователя
        return Task.CompletedTask;
    }

    private async Task OnMessage(Update update, CancellationToken ct)
    {
        var text = GetMessageFromUpdate(update)?.Trim();
        var chatId = GetChatFromUpdate(update);
        if (string.IsNullOrEmpty(text))
            return;

        _userData = await _botUsersService.GetUserAsync(GetUserIdFromUpdate(update), ct);
        _replyKeyboardMarkup = ConstantData.CreateReplyKeyboardMarkup(_userData);

        if (_userData != null && _userData.Status == UserStatus.Blocked)
        {
            await _telegramBotClient.SendMessage(chatId, "Вы заблокировны.", cancellationToken: ct);
            return;
        }
        else if(_userData == null)
        {
            await _telegramBotClient.SendMessage(chatId, $"Для запуска бота введите '{ConstantData.Start}'", replyMarkup: _replyKeyboardMarkup, cancellationToken: ct);
            return;
        }

        //_replyKeyboardMarkup = ConstantData.CreateReplyKeyboardMarkup(_userData);

        switch (text)
        {
            case ConstantData.Start:
                if (_userData == null)
                {
                    _userData = await _botUsersService.RegisterUserAsync(update.Message.From.Id,
                                                                    update.Message.From.Username,
                                                                    ct
                                                                    );
                    _replyKeyboardMarkup = ConstantData.CreateReplyKeyboardMarkup(_userData);
                    await _telegramBotClient.SendMessage(chatId,
                                                ConstantData.ReplaceText("Доступны новые команды", _userData),
                                                replyMarkup: _replyKeyboardMarkup,
                                                cancellationToken: ct);
                }
                break;
            case ConstantData.Pending:
                if (_userData == null) break;
                if (ConstantData.CommandsDictionary[$"{text}"].Levels.Contains(_userData.Status))
                {
                    SendErrorComand(chatId, text,_userData,_replyKeyboardMarkup,ct);
                    break;
                }


                //отправить запрос RequestElevationAsync и текущий статус пользователя
                //отправить результат запроса и текущий статус пользователя
                break;

            case ConstantData.ListServers:
                if (_userData == null) break;
                if (ConstantData.CommandsDictionary[$"{text}"].Levels.Contains(_userData.Status))
                {
                    SendErrorComand(chatId, text, _userData, _replyKeyboardMarkup, ct);
                    break;
                }

                //отправить запрос GetAccessibleServersAsync и текущий статус пользователя
                //отправить список серверов и текущий статус пользователя
                break;

            case ConstantData.ListScripts:
                if (_userData == null) break;
                if (ConstantData.CommandsDictionary[$"{text}"].Levels.Contains(_userData.Status))
                {
                    SendErrorComand(chatId, text, _userData, _replyKeyboardMarkup, ct);
                    break;
                }

                //отправить запрос GetAllScriptsAsync и текущий статус пользователя
                //отправить список скриптов и текущий статус пользователя
                break;

            case ConstantData.PendingRequests:
                if (_userData == null) break;
                if (ConstantData.CommandsDictionary[$"{text}"].Levels.Contains(_userData.Status))
                {
                    SendErrorComand(chatId, text, _userData, _replyKeyboardMarkup, ct);
                    break;
                }

                //отправить запрос GetPendingElevationRequestsAsync и текущий статус пользователя
                //отправить список заявок на повышение и текущий статус пользователя
                break;

            case ConstantData.AddServer:
                if (_userData == null) break;
                if (ConstantData.CommandsDictionary[$"{text}"].Levels.Contains(_userData.Status))
                {
                    SendErrorComand(chatId, text, _userData, _replyKeyboardMarkup, ct);
                    break;
                }

                //отправить запрос на запуск сценария добавления сервера и текущий статус пользователя
                break;

            default:
                //отправить сообщение о неизвестной команде и текущий статус пользователя
                break;
        }
    }

    
    internal static Chat GetChatFromUpdate(Update update)
    {
        if (update.Message != null)
            return update.Message.Chat;

        if (update.CallbackQuery?.Message != null)
            return update.CallbackQuery.Message.Chat;

        if (update.EditedMessage != null)
            return update.EditedMessage.Chat;

        throw new InvalidOperationException("Не удалось определить чат из update");
    }

    internal static long GetChatIdFromUpdate(Update update)
        => GetChatFromUpdate(update).Id;

    internal static string? GetMessageFromUpdate(Update update)
    {
        if (update.Message?.Text != null)
            return update.Message.Text;

        if (update.CallbackQuery?.Message?.Text != null)
            return update.CallbackQuery.Message.Text;

        if (update.EditedMessage?.Text != null)
            return update.EditedMessage.Text;

        return null;
    }

    internal static long GetUserIdFromUpdate(Update update)
    {
        if (update.Message?.From != null)
            return update.Message.From.Id;

        if (update.CallbackQuery?.From != null)
            return update.CallbackQuery.From.Id;

        if (update.EditedMessage?.From != null)
            return update.EditedMessage.From.Id;

        throw new InvalidOperationException("Не удалось получить Id пользователя из update");
    }

    private void SendErrorComand(Chat chat, string text, BotUser userData, ReplyKeyboardMarkup replyKeyboardMarkup, CancellationToken ct)
    {
        _telegramBotClient.SendMessage( chat,
                                        ConstantData.ReplaceText(
                                            $"Команда '{text}' не найдена. Доступные команды:\r\n {ConstantData.HelpData}",
                                            userData
                                        ),
                                        replyMarkup: replyKeyboardMarkup,
                                        cancellationToken: ct
                                        );
    }

}
