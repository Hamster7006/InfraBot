using InfraBot.Core.Interface.Services;
using InfraBot.HelpData;
using InfraBot.Scenarios.Core;
using InfraBot.TelegramBot;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace InfraBot.Scenarios.Tasks.SvcSamAccount;

/// <summary>
/// Удаление WinRM УЗ без подтверждения. Отмена, если УЗ привязана к серверу.
/// </summary>
internal sealed class DeleteSvcSamAccountScenario : IScenario
{
    private readonly ISvcSamAccountService _svcAccounts;
    private readonly IServerService _servers;
    private readonly IBotUserService _users;

    public DeleteSvcSamAccountScenario(
        ISvcSamAccountService svcAccounts,
        IServerService servers,
        IBotUserService users)
    {
        _svcAccounts = svcAccounts;
        _servers = servers;
        _users = users;
    }

    public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.DeleteSvcSamAccount;

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

        if (!context.Data.TryGetValue("SvcSamAccountId", out var idObj) || idObj is not Guid accountId)
            return ScenarioResult.Completed;

        var account = await _svcAccounts.GetAsync(accountId, ct);
        if (account == null)
        {
            await bot.EditMessageText(
                chat,
                messageId,
                ConstantData.ReplaceText("Учётная запись не найдена.", user),
                cancellationToken: ct);
            return ScenarioResult.Completed;
        }

        var linkedServers = await _servers.GetServersBySvcAccountAsync(accountId, ct);
        if (linkedServers.Count > 0)
        {
            var serverNames = string.Join(", ", linkedServers.Select(s => s.ServerName));
            await bot.EditMessageText(
                chat,
                messageId,
                ConstantData.ReplaceText(
                    $"Учётная запись «{account.SamAccountName}» привязана к серверам: {serverNames}.\r\n" +
                    "Удаление отменено.",
                    user),
                cancellationToken: ct);
            return ScenarioResult.Completed;
        }

        await _svcAccounts.DeleteAsync(accountId, ct);
        await bot.EditMessageText(
            chat,
            messageId,
            ConstantData.ReplaceText($"Учётная запись «{account.SamAccountName}» удалена.", user),
            cancellationToken: ct);
        return ScenarioResult.Completed;
    }
}
