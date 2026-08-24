using InfraBot.Entities;
using InfraBot.Enums;

namespace InfraBot.Helpers;

internal static class BotUserFormatter
{
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
