using LinqToDB.Mapping;

namespace InfraBot.Core.DataAccess.Models;

[Table("servers")]
internal class ServerModel
{
    [PrimaryKey]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("server_name")]
    public string ServerName { get; set; } = string.Empty;

    [Column("ip_address")]
    public string IpAddress { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("registered_by_user_id")]
    public Guid RegisteredByUserId { get; set; }

    [Column("win_rm_port")]
    public int WinRmPort { get; set; }

    [Column("svc_sam_account_id")]
    public Guid SvcSamAccountId { get; set; }
}
