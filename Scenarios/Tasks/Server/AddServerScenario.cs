using InfraBot.Core.Exceptions;
using InfraBot.Core.Interface.Services;
using InfraBot.HelpData;
using InfraBot.Scenarios.Core;
using InfraBot.TelegramBot;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace InfraBot.Scenarios.Tasks.Server;

/// <summary>
/// Добавление сервера: имя → IP → выбор УЗ → создание карточки.
/// </summary>
internal sealed class AddServerScenario : IScenario
{
    private const string SvcCallbackPrefix = "addserver|svc|";

    private readonly IServerService _servers;
    private readonly IBotUserService _users;
    private readonly ISvcSamAccountService _svcAccounts;

    public AddServerScenario(IServerService servers, IBotUserService users, ISvcSamAccountService svcAccounts)
    {
        _servers = servers;
        _users = users;
        _svcAccounts = svcAccounts;
    }

    public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.Server;

    public async Task<ScenarioResult> HandleMessageAsync(
        ITelegramBotClient bot,
        ScenarioContext context,
        Update update,
        CancellationToken ct)
    {
        if (context.Data.TryGetValue("Action", out var action)
            && action is ScenarioAction scenarioAction
            && scenarioAction != ScenarioAction.Create)
        {
            return ScenarioResult.Completed;
        }

        var chat = UpdateHandler.GetChatFromUpdate(update);
        var user = await _users.GetUserAsync(UpdateHandler.GetUserIdFromUpdate(update), ct);
        if (user == null)
            return ScenarioResult.Completed;

        var cancelKeyboard = ConstantData.CreateCancelKeyboard();
        var defaultKeyboard = ConstantData.CreateReplyKeyboardMarkup(user);
        var inputText = update.Message?.Text?.Trim() ?? string.Empty;
        var callbackData = update.CallbackQuery?.Data;

        if (inputText == ConstantData.Cancel)
            context.CurrentStep = "Cancel";

        switch (context.CurrentStep)
        {
            case null:
                context.Data["Action"] = ScenarioAction.Create;
                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText("Введите имя сервера:", user),
                    replyMarkup: cancelKeyboard,
                    cancellationToken: ct);
                context.CurrentStep = "Name";
                return ScenarioResult.Transition;

            case "Name":
                if (string.IsNullOrWhiteSpace(inputText))
                {
                    await bot.SendMessage(chat, "Имя не может быть пустым. Введите имя сервера:", cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                if (await _servers.ExistsByNameAsync(inputText, ct))
                {
                    await bot.SendMessage(
                        chat,
                        $"Сервер «{inputText}» уже существует. Введите другое имя:",
                        cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                context.Data["ServerName"] = inputText;
                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText("Введите IP-адрес сервера:", user),
                    replyMarkup: cancelKeyboard,
                    cancellationToken: ct);
                context.CurrentStep = "IP";
                return ScenarioResult.Transition;

            case "IP":
                if (string.IsNullOrWhiteSpace(inputText))
                {
                    await bot.SendMessage(chat, "IP-адрес не может быть пустым. Введите IP-адрес сервера:", cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                context.Data["ServerIp"] = inputText;

                var accounts = await _svcAccounts.GetAllAsync(ct);
                if (accounts.Count == 0)
                {
                    await bot.SendMessage(
                        chat,
                        ConstantData.ReplaceText(
                            "Нет доступных учётных записей. Сначала добавьте УЗ для WinRM.",
                            user),
                        replyMarkup: defaultKeyboard,
                        cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                var accountKeyboard = new InlineKeyboardMarkup();
                foreach (var account in accounts.OrderBy(a => a.SamAccountName))
                {
                    accountKeyboard.AddNewRow(
                        InlineKeyboardButton.WithCallbackData(
                            account.SamAccountName,
                            $"{SvcCallbackPrefix}{account.Id}"));
                }

                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText("Выберите учётную запись для WinRM:", user),
                    replyMarkup: accountKeyboard,
                    cancellationToken: ct);
                context.CurrentStep = "SelectAccount";
                return ScenarioResult.Transition;

            case "SelectAccount":
                if (string.IsNullOrEmpty(callbackData) || update.CallbackQuery?.Message == null)
                    return ScenarioResult.Transition;

                if (!callbackData.StartsWith(SvcCallbackPrefix, StringComparison.Ordinal)
                    || !Guid.TryParse(callbackData[SvcCallbackPrefix.Length..], out var accountId))
                {
                    return ScenarioResult.Transition;
                }

                var selectedAccount = await _svcAccounts.GetAsync(accountId, ct);
                if (selectedAccount == null)
                {
                    await bot.SendMessage(chat, "Учётная запись не найдена.", cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                await bot.EditMessageText(
                    chat,
                    update.CallbackQuery.Message.MessageId,
                    ConstantData.ReplaceText($"Выбрана УЗ: {selectedAccount.SamAccountName}", user),
                    cancellationToken: ct);

                context.Data["SvcSamAccountId"] = selectedAccount.Id;

                var serverName = context.Data["ServerName"] as string ?? string.Empty;
                var serverIp = context.Data["ServerIp"] as string ?? string.Empty;
                if (!context.Data.TryGetValue("SvcSamAccountId", out var svcObj) || svcObj is not Guid svcAccountId)
                {
                    await bot.SendMessage(chat, "Не выбрана учётная запись WinRM.", cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                var server = new Entities.Server(serverName, user.Id)
                {
                    IpAddress = serverIp,
                    SvcSamAccountId = svcAccountId
                };

                try
                {
                    await _servers.AddServerAsync(server, ct);
                    await bot.SendMessage(
                        chat,
                        ConstantData.ReplaceText($"Сервер «{server.ServerName}» создан.", user),
                        replyMarkup: defaultKeyboard,
                        cancellationToken: ct);
                }
                catch (InfraBotException ex)
                {
                    await bot.SendMessage(chat, ConstantData.ReplaceText(ex.Message, user), cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                return ScenarioResult.Completed;

            case "Cancel":
                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText("Добавление сервера отменено.", user),
                    replyMarkup: defaultKeyboard,
                    cancellationToken: ct);
                return ScenarioResult.Completed;

            default:
                return ScenarioResult.Completed;
        }
    }
}
