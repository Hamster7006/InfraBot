using System.Text.Json.Serialization;
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
    /// <summary>
    /// 
    /// </summary>
    /// <param name="script">ID Скрипта</param>
    /// <param name="server">ID сервера</param>
    /// <param name="initiatedBy">ID запустившего</param>
    public JobRun(Guid script, Guid server, Guid initiatedBy)
    {
        Id = Guid.NewGuid();
        Status = JobRunStatus.Queued;
        CreatedAt = DateTime.UtcNow;
        ScriptId = script;
        ServerId = server;
        InitiatedById = initiatedBy;
    }
}
