using InfraBot.Enums;

namespace InfraBot.Entities;

/// <summary>
/// Запуск PowerShell-скрипта на сервере.
/// </summary>
public class JobRun
{
    /// <summary>Уникальный идентификатор задачи.</summary>
    public Guid Id { get; set; }

    /// <summary>Текущий статус задачи.</summary>
    public JobRunStatus Status { get; set; }

    /// <summary>Результат выполнения скрипта в формате JSON.</summary>
    public string? ResultJson { get; set; }

    /// <summary>Текст ошибки при Status = Failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Код завершения процесса PowerShell.</summary>
    public int? ExitCode { get; set; }

    /// <summary>Дата постановки задачи в очередь (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Дата начала выполнения (UTC).</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>Дата завершения выполнения (UTC).</summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>Идентификатор скрипта (FK → Script).</summary>
    public Guid ScriptId { get; set; }

    /// <summary>Идентификатор сервера (FK → Server).</summary>
    public Guid ServerId { get; set; }

    /// <summary>Идентификатор пользователя, инициировавшего запуск (FK → BotUser).</summary>
    public Guid InitiatedById { get; set; }

    /// <summary>Id чата Telegram для отправки результата выполнения.</summary>
    public long ChatId { get; set; }

    public JobRun()
    {
    }

    /// <summary>Создаёт задачу в статусе Queued.</summary>
    public JobRun(Guid scriptId, Guid serverId, Guid initiatedById, long chatId)
    {
        Id = Guid.NewGuid();
        Status = JobRunStatus.Queued;
        CreatedAt = DateTime.UtcNow;
        ScriptId = scriptId;
        ServerId = serverId;
        InitiatedById = initiatedById;
        ChatId = chatId;
    }
}
