using InfraBot.Core.Interface.Services;
using InfraBot.HelpData;
using InfraBot.Scenarios.Core;
using InfraBot.TelegramBot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace InfraBot.Scenarios.Tasks.Server;

/// <summary>
/// Удаление сервера: одно подтверждение → удаление JobRun → удаление сервера.
/// </summary>
internal sealed class DeleteServerScenario : IScenario
{
    private const string ConfirmYesCallback = "deleteserver|confirm|yes";
    private const string ConfirmNoCallback = "deleteserver|confirm|no";

    private readonly IServerService _servers;
    private readonly IJobRunService _jobRuns;
    private readonly IBotUserService _users;

    public DeleteServerScenario(IServerService servers, IJobRunService jobRuns, IBotUserService users)
    {
        _servers = servers;
        _jobRuns = jobRuns;
        _users = users;
    }

    public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.DeleteServer;

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
                if (!context.Data.TryGetValue("ServerId", out var idObj) || idObj is not Guid serverId)
                    return ScenarioResult.Completed;

                var server = await _servers.GetServerAsync(serverId, ct);
                if (server == null)
                {
                    await bot.EditMessageText(
                        chat,
                        messageId,
                        ConstantData.ReplaceText("Сервер не найден.", user),
                        cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                context.Data["ServerName"] = server.ServerName;
                var keyboard = new InlineKeyboardMarkup();
                keyboard.AddNewRow(
                    InlineKeyboardButton.WithCallbackData("✅ Да", ConfirmYesCallback),
                    InlineKeyboardButton.WithCallbackData("❌ Нет", ConfirmNoCallback));

                await bot.EditMessageText(
                    chat,
                    messageId,
                    ConstantData.ReplaceText(
                        $"Удалить сервер «{server.ServerName}» и все связанные задачи?",
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
                        ConstantData.ReplaceText("Удаление сервера отменено.", user),
                        cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                if (callbackData != ConfirmYesCallback)
                    return ScenarioResult.Transition;

                if (!context.Data.TryGetValue("ServerId", out var deleteIdObj) || deleteIdObj is not Guid deleteServerId)
                    return ScenarioResult.Completed;

                var serverName = context.Data.TryGetValue("ServerName", out var nameObj) && nameObj is string name
                    ? name
                    : deleteServerId.ToString();

                await _jobRuns.DeleteJobsByServerAsync(deleteServerId, ct);
                await _servers.DeleteServerAsync(deleteServerId, ct);
                await bot.EditMessageText(
                    chat,
                    messageId,
                    ConstantData.ReplaceText($"Сервер «{serverName}» удалён.", user),
                    cancellationToken: ct);
                return ScenarioResult.Completed;

            default:
                return ScenarioResult.Completed;
        }
    }
}
