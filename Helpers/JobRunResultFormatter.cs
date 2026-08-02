using InfraBot.Enums;

namespace InfraBot.Helpers;

internal static class JobRunResultFormatter
{
    private const int MaxResultLength = 3500;

    internal static string BuildCompletionMessage(Guid jobId, JobRunStatus status, string? resultJson, string? errorMessage)
    {
        var resultText = FormatResult(status, resultJson, errorMessage);
        return $"Задача {jobId} завершена со статусом {FormatStatus(status)}\r\n" +
               $"Результат: {resultText}";
    }

    private static string FormatResult(JobRunStatus status, string? resultJson, string? errorMessage)
    {
        if (status == JobRunStatus.Failed)
            return string.IsNullOrWhiteSpace(errorMessage) ? "(ошибка без описания)" : TrimResult(errorMessage);

        if (string.IsNullOrWhiteSpace(resultJson))
            return "(пусто)";

        return TrimResult(resultJson);
    }

    private static string FormatStatus(JobRunStatus status) => status switch
    {
        JobRunStatus.Queued => "В очереди",
        JobRunStatus.Running => "Выполняется",
        JobRunStatus.Success => "Успех",
        JobRunStatus.Failed => "Ошибка",
        JobRunStatus.Cancelled => "Отменено",
        _ => status.ToString()
    };

    private static string TrimResult(string text)
        => text.Length <= MaxResultLength ? text : text[..MaxResultLength] + "...";
}
