using InfraBot.Entities.Enums;

namespace InfraBot.Entities;

/// <summary>
/// Запуск PowerShell-скрипта на сервере.
/// Результат выполнения хранится в JSON.
/// </summary>
public class JobRun
{
    /// <summary>Уникальный идентификатор задачи.</summary>
    public Guid Id { get; set; }
    /// <summary>Текущий статус задачи (Queued, Running, Success, Failed, Cancelled).</summary>
    public JobRunStatus Status { get; set; }
    /// <summary>
    /// Результат выполнения скрипта в формате JSON.
    /// </summary>
    public string? ResultJson { get; set; }

    /// <summary>Код завершения процесса PowerShell (0 = успех).</summary>
    public int? ExitCode { get; set; }

    /// <summary>Дата и время постановки задачи в очередь (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Дата и время начала выполнения (UTC).</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>Дата и время завершения выполнения (UTC).</summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>Запущенный скрипт.</summary>
    public Script Script { get; set; }

    /// <summary>Сервер, на котором выполнялся скрипт.</summary>
    public Server Server { get; set; }

    /// <summary>Пользователь, инициировавший запуск.</summary>
    public BotUser InitiatedBy { get; set; }
    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="script"></param>
    /// <param name="server"></param>
    /// <param name="initiatedBy"></param>
    public JobRun (Script script, Server server, BotUser initiatedBy)
    {
        Id = Guid.NewGuid ();
        Status = JobRunStatus.Queued;
        CreatedAt = DateTime.UtcNow;
        Script = script;
        Server = server;
        InitiatedBy = initiatedBy;
    }
}
