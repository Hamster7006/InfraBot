using LinqToDB.Mapping;

namespace InfraBot.Core.DataAccess.Models;

[Table("svc_sam_accounts")]
internal class SvcSamAccountModel
{
    [PrimaryKey]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("sam_account_name")]
    public string SamAccountName { get; set; } = string.Empty;

    [Column("password")]
    public string Password { get; set; } = string.Empty;
}
