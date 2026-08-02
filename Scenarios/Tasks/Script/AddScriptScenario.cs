using InfraBot.Core.Exceptions;
using InfraBot.Core.Interface.Services;
using InfraBot.HelpData;
using InfraBot.Scenarios.Core;
using InfraBot.TelegramBot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace InfraBot.Scenarios.Tasks.Script;

/// <summary>
/// Добавление скрипта: имя → описание → текст → ReturnData → таймаут → создание.
/// </summary>
internal sealed class AddScriptScenario : IScenario
{
    private const string ReturnDataYesCallback = "addscript|returndata|yes";
    private const string ReturnDataNoCallback = "addscript|returndata|no";

    private readonly IScriptService _scripts;
    private readonly IBotUserService _users;

    public AddScriptScenario(IScriptService scripts, IBotUserService users)
    {
        _scripts = scripts;
        _users = users;
    }

    public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.Script;

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
                    ConstantData.ReplaceText("Введите имя скрипта:", user),
                    replyMarkup: cancelKeyboard,
                    cancellationToken: ct);
                context.CurrentStep = "Name";
                return ScenarioResult.Transition;

            case "Name":
                if (string.IsNullOrWhiteSpace(inputText))
                {
                    await bot.SendMessage(chat, "Имя не может быть пустым. Введите имя скрипта:", cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                if (await _scripts.ExistsByNameAsync(inputText, null, ct))
                {
                    await bot.SendMessage(
                        chat,
                        $"Скрипт «{inputText}» уже существует. Введите другое имя:",
                        cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                context.Data["ScriptName"] = inputText;
                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText("Введите описание скрипта (или «-» чтобы пропустить):", user),
                    replyMarkup: cancelKeyboard,
                    cancellationToken: ct);
                context.CurrentStep = "Description";
                return ScenarioResult.Transition;

            case "Description":
                context.Data["ScriptDescription"] = inputText == "-" ? null : inputText;
                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText("Введите текст PowerShell-скрипта:", user),
                    replyMarkup: cancelKeyboard,
                    cancellationToken: ct);
                context.CurrentStep = "Content";
                return ScenarioResult.Transition;

            case "Content":
                if (string.IsNullOrWhiteSpace(inputText))
                {
                    await bot.SendMessage(chat, "Текст скрипта не может быть пустым.", cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                context.Data["ScriptContent"] = inputText;
                var returnDataKeyboard = new InlineKeyboardMarkup();
                returnDataKeyboard.AddNewRow(
                    InlineKeyboardButton.WithCallbackData("✅ Да", ReturnDataYesCallback),
                    InlineKeyboardButton.WithCallbackData("❌ Нет", ReturnDataNoCallback)
                );
                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText("Скрипт возвращает данные в JSON?", user),
                    replyMarkup: returnDataKeyboard,
                    cancellationToken: ct);
                context.CurrentStep = "ReturnData";
                return ScenarioResult.Transition;

            case "ReturnData":
                if (string.IsNullOrEmpty(callbackData) || update.CallbackQuery?.Message == null)
                    return ScenarioResult.Transition;

                context.Data["ReturnData"] = callbackData switch
                {
                    ReturnDataYesCallback => true,
                    ReturnDataNoCallback => false,
                    _ => context.Data.TryGetValue("ReturnData", out var v) && v is bool b && b
                };

                if (callbackData is not (ReturnDataYesCallback or ReturnDataNoCallback))
                    return ScenarioResult.Transition;

                await bot.SendMessage(
                    chat,
                    ConstantData.ReplaceText("Введите таймаут выполнения в секундах (по умолчанию 120):", user),
                    replyMarkup: cancelKeyboard,
                    cancellationToken: ct);
                context.CurrentStep = "Timeout";
                return ScenarioResult.Transition;

            case "Timeout":
                if (string.IsNullOrEmpty(inputText))
                {
                    context.Data["TimeoutSeconds"] = 120;
                }
                else if (!int.TryParse(inputText, out var timeout) || timeout <= 0)
                {
                    await bot.SendMessage(
                        chat,
                        "Введите положительное число секунд:",
                        replyMarkup: cancelKeyboard,
                        cancellationToken: ct);
                    return ScenarioResult.Transition;
                }
                else
                {
                    context.Data["TimeoutSeconds"] = timeout;
                }

                var name = context.Data["ScriptName"] as string ?? string.Empty;
                var description = context.Data.TryGetValue("ScriptDescription", out var descObj)
                    ? descObj as string
                    : null;
                var content = context.Data["ScriptContent"] as string ?? string.Empty;
                var returnData = context.Data.TryGetValue("ReturnData", out var rdObj)
                    && rdObj is bool rd && rd;
                var timeoutSeconds = context.Data.TryGetValue("TimeoutSeconds", out var toObj) && toObj is int t
                    ? t
                    : 120;

                if (string.IsNullOrWhiteSpace(content))
                {
                    await bot.SendMessage(chat, "Текст PowerShell-скрипта обязателен.", cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                var script = new Entities.Script(user.Id, name, content, description)
                {
                    ReturnData = returnData,
                    TimeoutSeconds = timeoutSeconds
                };

                try
                {
                    await _scripts.AddScriptAsync(script, ct);
                    await bot.SendMessage(
                        chat,
                        ConstantData.ReplaceText($"Скрипт «{script.Name}» создан.", user),
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
                    ConstantData.ReplaceText("Добавление скрипта отменено.", user),
                    replyMarkup: defaultKeyboard,
                    cancellationToken: ct);
                return ScenarioResult.Completed;

            default:
                return ScenarioResult.Completed;
        }
    }
}
