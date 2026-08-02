using InfraBot.Core.Exceptions;
using InfraBot.Core.Interface.Services;
using InfraBot.Entities;
using InfraBot.HelpData;
using InfraBot.Scenarios.Core;
using InfraBot.TelegramBot;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace InfraBot.Scenarios.Tasks.SvcSamAccount;

/// <summary>
/// Добавление WinRM УЗ: логин → пароль (сообщение удаляется) → создание.
/// </summary>
internal sealed class AddSvcSamAccountScenario : IScenario
{
    private const string LoginExample = "CORP\\svc_winrm или svc_winrm@corp.local";

    private readonly ISvcSamAccountService _svcAccounts;
    private readonly IBotUserService _users;

    public AddSvcSamAccountScenario(ISvcSamAccountService svcAccounts, IBotUserService users)
    {
        _svcAccounts = svcAccounts;
        _users = users;
    }

    public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.SvcSamAccount;

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

        if (inputText == ConstantData.Cancel)
            context.CurrentStep = "Cancel";

        switch (context.CurrentStep)
        {
            case null:
                context.Data["Action"] = ScenarioAction.Create;
                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText(
                        $"Введите логин учётной записи WinRM.\r\nПример: {LoginExample}",
                        user),
                    replyMarkup: cancelKeyboard,
                    cancellationToken: ct);
                context.CurrentStep = "Login";
                return ScenarioResult.Transition;

            case "Login":
                if (string.IsNullOrWhiteSpace(inputText))
                {
                    await bot.SendMessage(
                        chat,
                        $"Логин не может быть пустым. Пример: {LoginExample}",
                        cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                if (await _svcAccounts.ExistsBySamAccountNameAsync(inputText, null, ct))
                {
                    await bot.SendMessage(
                        chat,
                        $"Учётная запись «{inputText}» уже существует. Введите другой логин:",
                        cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                context.Data["SamAccountName"] = inputText;
                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText(
                        "Введите пароль. Сообщение с паролем будет удалено из чата.",
                        user),
                    replyMarkup: cancelKeyboard,
                    cancellationToken: ct);
                context.CurrentStep = "Password";
                return ScenarioResult.Transition;

            case "Password":
                if (string.IsNullOrWhiteSpace(inputText))
                {
                    await bot.SendMessage(chat, "Пароль не может быть пустым.", cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                await TryDeletePasswordMessageAsync(bot, update, chat, ct);

                var samAccountName = context.Data["SamAccountName"] as string ?? string.Empty;
                var account = new Entities.SvcSamAccount(samAccountName, inputText);

                try
                {
                    await _svcAccounts.AddAsync(account, ct);
                    await bot.SendMessage(
                        chat,
                        ConstantData.ReplaceText($"Учётная запись «{account.SamAccountName}» создана.", user),
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
                    ConstantData.ReplaceText("Добавление учётной записи отменено.", user),
                    replyMarkup: defaultKeyboard,
                    cancellationToken: ct);
                return ScenarioResult.Completed;

            default:
                return ScenarioResult.Completed;
        }
    }

    private static async Task TryDeletePasswordMessageAsync(
        ITelegramBotClient bot,
        Update update,
        Chat chat,
        CancellationToken ct)
    {
        if (update.Message == null)
            return;

        try
        {
            await bot.DeleteMessage(chat, update.Message.MessageId, cancellationToken: ct);
        }
        catch
        {
            // Бот может не иметь прав на удаление — пароль останется в истории чата.
        }
    }
}
