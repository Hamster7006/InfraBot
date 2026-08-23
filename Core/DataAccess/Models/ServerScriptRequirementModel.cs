using LinqToDB.Mapping;

namespace InfraBot.Core.DataAccess.Models;

[Table("server_script_requirements")]
internal class ServerScriptRequirementModel
{
    [Column("server_id"), PrimaryKey(1)]
    public Guid ServerId { get; set; }

    [Column("script_id"), PrimaryKey(2)]
    public Guid ScriptId { get; set; }
}
