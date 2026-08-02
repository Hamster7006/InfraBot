using InfraBot.Core.Interface.Services;
using InfraBot.HelpData;
using InfraBot.Scenarios.Core;
using InfraBot.TelegramBot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace InfraBot.Scenarios.Tasks.Script;

/// <summary>
/// Удаление скрипта: одно подтверждение со списком серверов → JobRun → отвязка → скрипт.
/// </summary>
internal sealed class DeleteScriptScenario : IScenario
{
    private const string ConfirmYesCallback = "deletescript|confirm|yes";
    private const string ConfirmNoCallback = "deletescript|confirm|no";

    private readonly IScriptService _scripts;
    private readonly IJobRunService _jobRuns;
    private readonly IServerService _servers;
    private readonly IBotUserService _users;

    public DeleteScriptScenario(
        IScriptService scripts,
        IJobRunService jobRuns,
        IServerService servers,
        IBotUserService users)
    {
        _scripts = scripts;
        _jobRuns = jobRuns;
        _servers = servers;
        _users = users;
    }

    public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.DeleteScript;

    public async Task<ScenarioResult> HandleMessageAsync(
        ITelegramBotClient bot,
        ScenarioContext context,
        Update update,
        CancellationToken ct)
    {
        var callbackQuery = update.CallbackQuery;
        if (callbackQuery?.Message == null)
            return ScenarioResult.Completed;

        var user = await _users.GetUserAsync(UpdateHandler.GetUserIdFromUpdate(update), ct);
        if (user == null)
            return ScenarioResult.Completed;

        var chat = callbackQuery.Message.Chat;
        var messageId = callbackQuery.Message.MessageId;
        var callbackData = callbackQuery.Data;

        switch (context.CurrentStep)
        {
            case null:
                if (!context.Data.TryGetValue("ScriptId", out var idObj) || idObj is not Guid scriptId)
                    return ScenarioResult.Completed;

                var script = await _scripts.GetScriptAsync(scriptId, ct);
                if (script == null)
                {
                    await bot.EditMessageText(
                        chat,
                        messageId,
                        ConstantData.ReplaceText("Скрипт не найден.", user),
                        cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                context.Data["ScriptName"] = script.Name;

                var linkedServers = await _servers.GetServersByScriptAsync(scriptId, ct);
                var serversText = linkedServers.Count > 0
                    ? string.Join(", ", linkedServers.Select(s => s.ServerName))
                    : "ни к одному серверу";

                var keyboard = new InlineKeyboardMarkup();
                keyboard.AddNewRow(
                    InlineKeyboardButton.WithCallbackData("✅ Да", ConfirmYesCallback),
                    InlineKeyboardButton.WithCallbackData("❌ Нет", ConfirmNoCallback));

                await bot.EditMessageText(
                    chat,
                    messageId,
                    ConstantData.ReplaceText(
                        $"Удалить скрипт «{script.Name}» и все связанные задачи?\r\n" +
                        $"Привязан к серверам: {serversText}",
                        user),
                    replyMarkup: keyboard,
                    cancellationToken: ct);
                context.CurrentStep = "Confirm";
                return ScenarioResult.Transition;

            case "Confirm":
                if (callbackData == ConfirmNoCallback)
                {
                    await bot.EditMessageText(
                        chat,
                        messageId,
                        ConstantData.ReplaceText("Удаление скрипта отменено.", user),
                        cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                if (callbackData != ConfirmYesCallback)
                    return ScenarioResult.Transition;

                if (!context.Data.TryGetValue("ScriptId", out var deleteIdObj) || deleteIdObj is not Guid deleteScriptId)
                    return ScenarioResult.Completed;

                var scriptName = context.Data.TryGetValue("ScriptName", out var nameObj) && nameObj is string name
                    ? name
                    : deleteScriptId.ToString();

                await _jobRuns.DeleteJobsByScriptAsync(deleteScriptId, ct);
                await _servers.RemoveScriptFromAllServersAsync(deleteScriptId, ct);
                await _scripts.DeleteScriptAsync(deleteScriptId, ct);
                await bot.EditMessageText(
                    chat,
                    messageId,
                    ConstantData.ReplaceText($"Скрипт «{scriptName}» удалён.", user),
                    cancellationToken: ct);
                return ScenarioResult.Completed;

            default:
                return ScenarioResult.Completed;
        }
    }
}
