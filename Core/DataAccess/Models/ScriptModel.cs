using LinqToDB.Mapping;

namespace InfraBot.Core.DataAccess.Models;

[Table("scripts")]
internal class ScriptModel
{
    [PrimaryKey]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("return_data")]
    public bool ReturnData { get; set; }

    [Column("timeout_seconds")]
    public int TimeoutSeconds { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by_id")]
    public Guid CreatedById { get; set; }
}
