namespace InfraBot.Entities;

/// <summary>Сводка для команды /info.</summary>
public sealed class UserInfoReport
{
    public int AccessibleServersCount { get; init; }
    public int JobRunsCount { get; init; }
}
