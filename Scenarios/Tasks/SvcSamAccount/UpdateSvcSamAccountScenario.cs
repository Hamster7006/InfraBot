using InfraBot.Core.Exceptions;
using InfraBot.Core.Interface.Services;
using InfraBot.HelpData;
using InfraBot.Scenarios.Core;
using InfraBot.TelegramBot;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace InfraBot.Scenarios.Tasks.SvcSamAccount;

/// <summary>
/// Смена пароля WinRM УЗ из карточки: запрос нового пароля → сохранение.
/// </summary>
internal sealed class UpdateSvcSamAccountScenario : IScenario
{
    private readonly ISvcSamAccountService _svcAccounts;
    private readonly IBotUserService _users;

    public UpdateSvcSamAccountScenario(ISvcSamAccountService svcAccounts, IBotUserService users)
    {
        _svcAccounts = svcAccounts;
        _users = users;
    }

    public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.UpdateSvcSamAccount;

    public async Task<ScenarioResult> HandleMessageAsync(
        ITelegramBotClient bot,
        ScenarioContext context,
        Update update,
        CancellationToken ct)
    {
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
                if (!context.Data.TryGetValue("SvcSamAccountId", out var idObj) || idObj is not Guid accountId)
                    return ScenarioResult.Completed;

                var account = await _svcAccounts.GetAsync(accountId, ct);
                if (account == null)
                {
                    await bot.SendMessage(chat, ConstantData.ReplaceText("Учётная запись не найдена.", user), cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                context.Data["SamAccountName"] = account.SamAccountName;
                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText(
                        $"Смена пароля для «{account.SamAccountName}».\r\n" +
                        "Введите новый пароль. Сообщение с паролем будет удалено из чата.",
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

                if (!context.Data.TryGetValue("SvcSamAccountId", out var updateIdObj) || updateIdObj is not Guid updateAccountId)
                    return ScenarioResult.Completed;

                var existing = await _svcAccounts.GetAsync(updateAccountId, ct);
                if (existing == null)
                {
                    await bot.SendMessage(chat, ConstantData.ReplaceText("Учётная запись не найдена.", user), cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                existing.Password = inputText;

                try
                {
                    await _svcAccounts.UpdateAsync(existing, ct);
                    await bot.SendMessage(
                        chat,
                        ConstantData.ReplaceText($"Пароль для «{existing.SamAccountName}» обновлён.", user),
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
                var accountName = context.Data.TryGetValue("SamAccountName", out var nameObj) && nameObj is string name
                    ? name
                    : "учётной записи";
                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText($"Смена пароля для «{accountName}» отменена.", user),
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
