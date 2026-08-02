using InfraBot.Scenarios.Core;
using System.Linq;
using Telegram.Bot.Types;

namespace InfraBot.TelegramBot;

internal partial class UpdateHandler
{
    private async Task ProcessScenarioAsync(ScenarioContext context, Update update, CancellationToken ct)
    {
        var scenario = GetScenario(context.currentScenario);
        if (await scenario.HandleMessageAsync(_telegramBotClient, context, update, ct) == ScenarioResult.Completed)
            await _scenarioContextRepository.ResetContext(GetUserIdFromUpdate(update), ct);
        else
            await _scenarioContextRepository.SetContext(GetUserIdFromUpdate(update), context, ct);
    }

    private IScenario GetScenario(ScenarioType scenarioType)
    {
        var scenarios = _scenarios.Where(x => x.CanHandle(scenarioType));
        if (scenarios.Any())
            return scenarios.First();

        throw new NullReferenceException($"Тип сценария {scenarioType} не найден.");
    }
}
