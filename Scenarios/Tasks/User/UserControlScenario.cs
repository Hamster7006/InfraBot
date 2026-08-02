using InfraBot.Core.Interface.Services;
using InfraBot.Entities;
using InfraBot.Enums;
using InfraBot.Scenarios.Core;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace InfraBot.Scenarios.Tasks.User;

/// <summary>
/// Сценарий User: зарезервирован для текстовых шагов управления пользователями.
/// Списки и смена роли — через inline-callback в UpdateHandler_Users.
/// </summary>
internal sealed class UserControlScenario : IScenario
{
    public UserControlScenario(IBotUserService users)
    {
    }

    public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.User;

    public Task<ScenarioResult> HandleMessageAsync(
        ITelegramBotClient bot,
        ScenarioContext context,
        Update update,
        CancellationToken ct)
        => Task.FromResult(ScenarioResult.Completed);

    /// <summary>Отображаемое имя: @username или TelegramId.</summary>
    internal static string FormatUserLabel(BotUser? user)
    {
        if (user == null)
            return "неизвестный";

        return string.IsNullOrWhiteSpace(user.Username)
            ? user.TelegramId.ToString()
            : $"@{user.Username}";
    }

    internal static string FormatUserStatus(UserStatus status) => status switch
    {
        UserStatus.Guest => "Guest",
        UserStatus.Operator => "Operator",
        UserStatus.Admin => "Admin",
        _ => status.ToString()
    };
}
