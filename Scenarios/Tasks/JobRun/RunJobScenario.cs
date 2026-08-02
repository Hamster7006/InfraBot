using InfraBot.Core.Exceptions;
using InfraBot.Core.Interface.Services;
using InfraBot.Entities;
using InfraBot.HelpData;
using InfraBot.Scenarios.Core;
using InfraBot.TelegramBot;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace InfraBot.Scenarios.Tasks.JobRun;

/// <summary>
/// Запуск JobRun: выбор скрипта → CreateJobAsync → очередь исполнителя.
/// Стартует из карточки сервера (ServerId в context.Data).
/// </summary>
internal sealed class RunJobScenario : IScenario
{
    private const string ScriptCallbackPrefix = "runjob|script|";

    private readonly IServerService _servers;
    private readonly IScriptService _scripts;
    private readonly IJobRunService _jobRuns;
    private readonly IJobRunExe _executor;
    private readonly IBotUserService _users;

    public RunJobScenario(
        IServerService servers,
        IScriptService scripts,
        IJobRunService jobRuns,
        IJobRunExe executor,
        IBotUserService users)
    {
        _servers = servers;
        _scripts = scripts;
        _jobRuns = jobRuns;
        _executor = executor;
        _users = users;
    }

    public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.RunJob;

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
        var callbackData = callbackQuery.Data ?? string.Empty;

        if (!context.Data.TryGetValue("ServerId", out var serverIdObject) || serverIdObject is not Guid serverId)
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

        if (context.CurrentStep == null)
        {
            var keyboard = await BuildScriptSelectionKeyboardAsync(server, ct);
            if (keyboard == null)
            {
                await bot.EditMessageText(
                    chat,
                    messageId,
                    ConstantData.ReplaceText(
                        $"На сервере «{server.ServerName}» нет доступных для запуска скриптов.",
                        user),
                    cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            await bot.EditMessageText(
                chat,
                messageId,
                ConstantData.ReplaceText(
                    $"Выберите скрипт для запуска на «{server.ServerName}»:",
                    user),
                replyMarkup: keyboard,
                cancellationToken: ct);

            context.CurrentStep = "SelectScript";
            return ScenarioResult.Transition;
        }

        if (context.CurrentStep == "SelectScript")
        {
            if (!callbackData.StartsWith(ScriptCallbackPrefix, StringComparison.Ordinal))
                return ScenarioResult.Transition;

            if (!Guid.TryParse(callbackData[ScriptCallbackPrefix.Length..], out var scriptId))
                return ScenarioResult.Transition;

            if (!server.ScriptRequirements.Contains(scriptId))
            {
                await bot.EditMessageText(
                    chat,
                    messageId,
                    ConstantData.ReplaceText("Скрипт недоступен для этого сервера.", user),
                    cancellationToken: ct);
                return ScenarioResult.Completed;
            }

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

            try
            {
                var job = await _jobRuns.CreateJobAsync(user, scriptId, serverId, chat, ct);
                await _executor.EnqueueAsync(job, ct);

                await bot.EditMessageText(
                    chat,
                    messageId,
                    ConstantData.ReplaceText(
                        $"Задача {job.Id} поставлена в очередь.\r\n" +
                        $"Скрипт: {script.Name}\r\n" +
                        $"Сервер: {server.ServerName}",
                        user),
                    cancellationToken: ct);

                var keyboard = await BuildScriptSelectionKeyboardAsync(server, ct);
                if (keyboard != null)
                {
                    await bot.SendMessage(
                        chat,
                        ConstantData.ReplaceText(
                            $"Выберите скрипт для запуска на «{server.ServerName}»:",
                            user),
                        replyMarkup: keyboard,
                        cancellationToken: ct);
                }
            }
            catch (InfraBotException exception)
            {
                await bot.EditMessageText(
                    chat,
                    messageId,
                    ConstantData.ReplaceText(exception.Message, user),
                    cancellationToken: ct);
            }

            return ScenarioResult.Completed;
        }

        return ScenarioResult.Completed;
    }

    private async Task<InlineKeyboardMarkup?> BuildScriptSelectionKeyboardAsync(Entities.Server server, CancellationToken ct)
    {
        var keyboard = new InlineKeyboardMarkup();
        var hasScripts = false;

        foreach (var scriptId in server.ScriptRequirements)
        {
            var script = await _scripts.GetScriptAsync(scriptId, ct);
            if (script == null)
                continue;

            hasScripts = true;
            keyboard.AddNewRow(
                InlineKeyboardButton.WithCallbackData(
                    script.Name,
                    $"{ScriptCallbackPrefix}{script.Id}"));
        }

        return hasScripts ? keyboard : null;
    }
}
