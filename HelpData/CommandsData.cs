using InfraBot.Enums;

namespace InfraBot.HelpData;

internal class CommandsData
{
    internal string Description { get; set; } = null!;
    internal List<UserStatus> Levels { get; set; } = [];
}
