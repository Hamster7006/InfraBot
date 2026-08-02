using InfraBot.Core.Interface.Services;
using InfraBot.HelpData;
using InfraBot.Scenarios.Core;
using InfraBot.TelegramBot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace InfraBot.Scenarios.Tasks.Script;

/// <summary>
/// Изменение скрипта из карточки: выбор атрибута → ввод/выбор → сохранение.
/// </summary>
internal sealed class UpdateScriptScenario : IScenario
{
    private const string AttrAction = "updatescriptattr";
    private const string ReturnDataYesCallback = "updatescriptattr|returndata|yes";
    private const string ReturnDataNoCallback = "updatescriptattr|returndata|no";

    private readonly IScriptService _scripts;
    private readonly IBotUserService _users;

    public UpdateScriptScenario(IScriptService scripts, IBotUserService users)
    {
        _scripts = scripts;
        _users = users;
    }

    public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.UpdateScript;

    public async Task<ScenarioResult> HandleMessageAsync(
        ITelegramBotClient bot,
        ScenarioContext context,
        Update update,
        CancellationToken ct)
    {
        var user = await _users.GetUserAsync(UpdateHandler.GetUserIdFromUpdate(update), ct);
        if (user == null)
            return ScenarioResult.Completed;

        var chat = UpdateHandler.GetChatFromUpdate(update);
        var defaultKeyboard = ConstantData.CreateReplyKeyboardMarkup(user);
        var cancelKeyboard = ConstantData.CreateCancelKeyboard();
        var inputText = update.Message?.Text?.Trim() ?? string.Empty;
        var callbackData = update.CallbackQuery?.Data;

        if (inputText == ConstantData.Cancel)
            context.CurrentStep = "Cancel";

        switch (context.CurrentStep)
        {
            case null:
                if (!context.Data.TryGetValue("ScriptId", out var startIdObj) || startIdObj is not Guid startScriptId)
                    return ScenarioResult.Completed;

                var startScript = await _scripts.GetScriptAsync(startScriptId, ct);
                if (startScript == null)
                {
                    await EditOrSendAsync(bot, update, chat, ConstantData.ReplaceText("Скрипт не найден.", user), null, ct);
                    return ScenarioResult.Completed;
                }

                context.Data["ScriptName"] = startScript.Name;
                var menuKeyboard = new InlineKeyboardMarkup();
                menuKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData("Имя", $"{AttrAction}|name"));
                menuKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData("Описание", $"{AttrAction}|description"));
                menuKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData("Показать текст скрипта", $"{AttrAction}|showcontent"));
                menuKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData("Изменить текст скрипта", $"{AttrAction}|content"));
                menuKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData("ReturnData (JSON)", $"{AttrAction}|returndata"));
                menuKeyboard.AddNewRow(InlineKeyboardButton.WithCallbackData("Таймаут", $"{AttrAction}|timeout"));
                await EditOrSendAsync(
                    bot,
                    update,
                    chat,
                    ConstantData.ReplaceText($"Выберите атрибут для изменения:\r\nСкрипт: «{startScript.Name}»", user),
                    menuKeyboard,
                    ct);
                context.CurrentStep = "SelectAttribute";
                return ScenarioResult.Transition;

            case "SelectAttribute":
                if (string.IsNullOrEmpty(callbackData) || !callbackData.StartsWith($"{AttrAction}|", StringComparison.Ordinal))
                    return ScenarioResult.Transition;

                var attribute = callbackData[(AttrAction.Length + 1)..];

                if (attribute is "returndata|yes" or "returndata|no")
                {
                    if (!context.Data.TryGetValue("ScriptId", out var rdIdObj) || rdIdObj is not Guid rdScriptId)
                        return ScenarioResult.Completed;

                    var rdScript = await _scripts.GetScriptAsync(rdScriptId, ct);
                    if (rdScript == null)
                        return ScenarioResult.Completed;

                    rdScript.ReturnData = attribute == "returndata|yes";
                    await _scripts.UpdateScriptAsync(rdScript, ct);
                    var rdText = rdScript.ReturnData ? "включён" : "отключён";
                    await EditOrSendAsync(
                        bot,
                        update,
                        chat,
                        ConstantData.ReplaceText($"ReturnData для «{rdScript.Name}» {rdText}.", user),
                        null,
                        ct);
                    return ScenarioResult.Completed;
                }

                switch (attribute)
                {
                    case "name":
                        await bot.SendMessage(
                            chat,
                            ConstantData.ReplaceText("Введите новое имя скрипта:", user),
                            replyMarkup: cancelKeyboard,
                            cancellationToken: ct);
                        context.CurrentStep = "EnterName";
                        return ScenarioResult.Transition;

                    case "description":
                        await bot.SendMessage(
                            chat,
                            ConstantData.ReplaceText("Введите новое описание (или «-» чтобы очистить):", user),
                            replyMarkup: cancelKeyboard,
                            cancellationToken: ct);
                        context.CurrentStep = "EnterDescription";
                        return ScenarioResult.Transition;

                    case "showcontent":
                        if (!context.Data.TryGetValue("ScriptId", out var showIdObj) || showIdObj is not Guid showScriptId)
                            return ScenarioResult.Completed;

                        var showScript = await _scripts.GetScriptAsync(showScriptId, ct);
                        if (showScript == null)
                            return ScenarioResult.Completed;

                        await bot.SendMessage(
                            chat,
                            ConstantData.ReplaceText(
                                $"Текст скрипта «{showScript.Name}»:\r\n{FormatScriptContent(showScript.Content)}",
                                user),
                            replyMarkup: defaultKeyboard,
                            cancellationToken: ct);
                        return ScenarioResult.Completed;

                    case "content":
                        await bot.SendMessage(
                            chat,
                            ConstantData.ReplaceText("Введите новый текст PowerShell-скрипта:", user),
                            replyMarkup: cancelKeyboard,
                            cancellationToken: ct);
                        context.CurrentStep = "EnterContent";
                        return ScenarioResult.Transition;

                    case "returndata":
                        var keyboard = new InlineKeyboardMarkup();
                        keyboard.AddNewRow(
                            InlineKeyboardButton.WithCallbackData("✅ Да", ReturnDataYesCallback),
                            InlineKeyboardButton.WithCallbackData("❌ Нет", ReturnDataNoCallback)
                        );
                        await EditOrSendAsync(
                            bot,
                            update,
                            chat,
                            ConstantData.ReplaceText("Скрипт возвращает данные в JSON?", user),
                            keyboard,
                            ct);
                        return ScenarioResult.Transition;

                    case "timeout":
                        await bot.SendMessage(
                            chat,
                            ConstantData.ReplaceText("Введите таймаут в секундах:", user),
                            replyMarkup: cancelKeyboard,
                            cancellationToken: ct);
                        context.CurrentStep = "EnterTimeout";
                        return ScenarioResult.Transition;

                    default:
                        return ScenarioResult.Transition;
                }

            case "EnterName":
                if (string.IsNullOrWhiteSpace(inputText))
                    return ScenarioResult.Transition;

                if (!context.Data.TryGetValue("ScriptId", out var nameIdObj) || nameIdObj is not Guid nameScriptId)
                    return ScenarioResult.Completed;

                if (await _scripts.ExistsByNameAsync(inputText, nameScriptId, ct))
                {
                    await bot.SendMessage(
                        chat,
                        $"Скрипт «{inputText}» уже существует. Введите другое имя:",
                        replyMarkup: cancelKeyboard,
                        cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                var nameScript = await _scripts.GetScriptAsync(nameScriptId, ct);
                if (nameScript == null)
                    return ScenarioResult.Completed;

                nameScript.Name = inputText;
                await _scripts.UpdateScriptAsync(nameScript, ct);
                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText($"Имя скрипта изменено на «{nameScript.Name}».", user),
                    replyMarkup: defaultKeyboard,
                    cancellationToken: ct);
                return ScenarioResult.Completed;

            case "EnterDescription":
                if (string.IsNullOrEmpty(inputText))
                    return ScenarioResult.Transition;

                if (!context.Data.TryGetValue("ScriptId", out var descIdObj) || descIdObj is not Guid descScriptId)
                    return ScenarioResult.Completed;

                var descScript = await _scripts.GetScriptAsync(descScriptId, ct);
                if (descScript == null)
                    return ScenarioResult.Completed;

                descScript.Description = inputText == "-" ? null : inputText;
                await _scripts.UpdateScriptAsync(descScript, ct);
                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText($"Описание скрипта «{descScript.Name}» обновлено.", user),
                    replyMarkup: defaultKeyboard,
                    cancellationToken: ct);
                return ScenarioResult.Completed;

            case "EnterContent":
                if (string.IsNullOrWhiteSpace(inputText))
                    return ScenarioResult.Transition;

                if (!context.Data.TryGetValue("ScriptId", out var contentIdObj) || contentIdObj is not Guid contentScriptId)
                    return ScenarioResult.Completed;

                var contentScript = await _scripts.GetScriptAsync(contentScriptId, ct);
                if (contentScript == null)
                    return ScenarioResult.Completed;

                contentScript.Content = inputText;
                await _scripts.UpdateScriptAsync(contentScript, ct);
                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText($"Текст скрипта «{contentScript.Name}» обновлён.", user),
                    replyMarkup: defaultKeyboard,
                    cancellationToken: ct);
                return ScenarioResult.Completed;

            case "EnterTimeout":
                if (string.IsNullOrEmpty(inputText) || !int.TryParse(inputText, out var timeout) || timeout <= 0)
                {
                    await bot.SendMessage(
                        chat,
                        "Введите положительное число секунд:",
                        replyMarkup: cancelKeyboard,
                        cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                if (!context.Data.TryGetValue("ScriptId", out var timeoutIdObj) || timeoutIdObj is not Guid timeoutScriptId)
                    return ScenarioResult.Completed;

                var timeoutScript = await _scripts.GetScriptAsync(timeoutScriptId, ct);
                if (timeoutScript == null)
                    return ScenarioResult.Completed;

                timeoutScript.TimeoutSeconds = timeout;
                await _scripts.UpdateScriptAsync(timeoutScript, ct);
                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText($"Таймаут скрипта «{timeoutScript.Name}» установлен: {timeout} сек.", user),
                    replyMarkup: defaultKeyboard,
                    cancellationToken: ct);
                return ScenarioResult.Completed;

            case "Cancel":
                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText("Изменение скрипта отменено.", user),
                    replyMarkup: defaultKeyboard,
                    cancellationToken: ct);
                return ScenarioResult.Completed;

            default:
                return ScenarioResult.Completed;
        }
    }

    private static async Task EditOrSendAsync(
        ITelegramBotClient bot,
        Update update,
        Chat chat,
        string text,
        InlineKeyboardMarkup? keyboard,
        CancellationToken ct)
    {
        if (update.CallbackQuery?.Message != null)
        {
            await bot.EditMessageText(
                chat,
                update.CallbackQuery.Message.MessageId,
                text,
                replyMarkup: keyboard,
                cancellationToken: ct);
        }
        else
        {
            await bot.SendMessage(chat, text, replyMarkup: keyboard, cancellationToken: ct);
        }
    }

    private static string FormatScriptContent(string content)
    {
        const int maxLength = 4000;
        return content.Length <= maxLength ? content : content[..maxLength] + "...";
    }
}
