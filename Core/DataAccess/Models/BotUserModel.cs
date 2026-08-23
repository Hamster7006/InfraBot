using InfraBot.Enums;
using LinqToDB.Mapping;

namespace InfraBot.Core.DataAccess.Models;

[Table("bot_users")]
internal class BotUserModel
{
    [PrimaryKey]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("telegram_id")]
    public long TelegramId { get; set; }

    [Column("username")]
    public string? Username { get; set; }

    [Column("status")]
    public UserStatus Status { get; set; }

    [Column("pending")]
    public UserPending Pending { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
