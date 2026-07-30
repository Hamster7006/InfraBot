using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace InfraBot.Scenarios.Core
{
    public interface IScenario
    {
        bool CanHandle(ScenarioType scenario);
        Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Update update, CancellationToken ct);
    }
}
