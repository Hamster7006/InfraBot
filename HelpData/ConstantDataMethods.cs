using InfraBot.Entities;
using InfraBot.Enums;
using InfraBot.Helpers;

namespace InfraBot.HelpData;

internal partial class ConstantData
{
        internal static string HelpData(BotUser user)
        {
            var temp = "";
            var metaCommands = new HashSet<string> { Help, Info, About, AdminControl };
            var available = CommandsDictionary
                .Where(x => x.Value.Levels.Contains(user.Status) && !metaCommands.Contains(x.Key))
                .ToDictionary();

            foreach (var key in available.Keys)
                temp += $"{key} - {available[key].Description}\r\n";

            return temp;
        }

        internal static string InfoData(BotUser user, UserInfoReport report)
        {
            var label = string.IsNullOrWhiteSpace(user.Username)
                ? user.TelegramId.ToString()
                : $"@{user.Username}";

            return $"Имя: {label}\r\n" +
                   $"Telegram ID: {user.TelegramId}\r\n" +
                   $"Роль: {BotUserFormatter.FormatUserStatus(user.Status)}\r\n" +
                   $"Доступные сервера: {report.AccessibleServersCount}\r\n" +
                   $"Запущено скриптов: {report.JobRunsCount}";
        }
        internal static string AboutData() =>
            "InfraBot — Telegram-бот для управления инфраструктурой.\r\n" +
            "Серверы, скрипты, роли пользователей.\r\n" +
            ".NET 8";

        static internal string ReplaceText(string text, BotUser? userName)
        {
            if (userName != null)
                text = $"{userName.Username},\r\n{text}";
            return text;
        }
}
