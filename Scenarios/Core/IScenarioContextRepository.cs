namespace InfraBot.Scenarios.Core;

/// <summary>Хранилище активного сценария пользователя (один сценарий на TelegramId).</summary>
public interface IScenarioContextRepository
{
    Task<ScenarioContext?> GetContext(long userId, CancellationToken ct);
    Task SetContext(long userId, ScenarioContext context, CancellationToken ct);
    Task ResetContext(long userId, CancellationToken ct);
}
