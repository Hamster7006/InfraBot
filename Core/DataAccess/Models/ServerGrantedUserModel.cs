using LinqToDB.Mapping;

namespace InfraBot.Core.DataAccess.Models;

[Table("server_granted_users")]
internal class ServerGrantedUserModel
{
    [Column("server_id"), PrimaryKey(1)]
    public Guid ServerId { get; set; }

    [Column("user_id"), PrimaryKey(2)]
    public Guid UserId { get; set; }
}
