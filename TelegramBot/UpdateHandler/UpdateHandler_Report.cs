using InfraBot.Entities;
using InfraBot.Enums;
using InfraBot.HelpData;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace InfraBot.TelegramBot;

internal partial class UpdateHandler
{
    private async Task SendJobReportAsync(bool allJobs, Chat chatId, CancellationToken ct)
    {
        var jobs = await _jobRunsService.ReportAsync(allJobs, _userData!, ct);

        var serverNames = new Dictionary<Guid, string>();
        foreach (var serverId in jobs.Select(j => j.ServerId).Distinct())
        {
            var server = await _serversService.GetServerAsync(serverId, ct);
            serverNames[serverId] = server?.ServerName ?? $"{serverId:N}"[..8];
        }
        var text = new StringBuilder();
        text.AppendLine(allJobs
            ? "Отчёт по всем запускам (7 дней)"
            : "Отчёт по вашим запускам (7 дней)");
        text.AppendLine($"Всего задач: {jobs.Count}");
        text.AppendLine();
        text.AppendLine("По статусам:");
        foreach (JobRunStatus status in Enum.GetValues<JobRunStatus>())
        {
            var count = jobs.Count(j => j.Status == status);
            if (count > 0)
                text.AppendLine($"  {FormatJobRunStatus(status)}: {count}");
        }
        await _telegramBotClient.SendMessage(
            chatId,
            ConstantData.ReplaceText(text.ToString().TrimEnd(), _userData),
            replyMarkup: _replyKeyboardMarkup,
            cancellationToken: ct);
    }


    private static string FormatJobRunStatus(JobRunStatus status) => status switch
    {
        JobRunStatus.Queued => "В очереди",
        JobRunStatus.Running => "Выполняется",
        JobRunStatus.Success => "Успех",
        JobRunStatus.Failed => "Ошибка",
        JobRunStatus.Cancelled => "Отменено",
        _ => status.ToString()
    };
}
