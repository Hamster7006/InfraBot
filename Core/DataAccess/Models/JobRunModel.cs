using InfraBot.Enums;
using LinqToDB.Mapping;

namespace InfraBot.Core.DataAccess.Models;

[Table("job_runs")]
internal class JobRunModel
{
    [PrimaryKey]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("status")]
    public JobRunStatus Status { get; set; }

    [Column("result_json")]
    public string? ResultJson { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("exit_code")]
    public int? ExitCode { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("finished_at")]
    public DateTime? FinishedAt { get; set; }

    [Column("script_id")]
    public Guid ScriptId { get; set; }

    [Column("server_id")]
    public Guid ServerId { get; set; }

    [Column("initiated_by_id")]
    public Guid InitiatedById { get; set; }

    [Column("chat_id")]
    public long ChatId { get; set; }
}
