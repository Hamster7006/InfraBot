using InfraBot.Enums;

namespace InfraBot.Entities;

/// <summary>
/// Пользователь Telegram-бота.
/// </summary>
public class BotUser
{
    /// <summary>Уникальный идентификатор в базе данных.</summary>
    public Guid Id { get; set; }

    /// <summary>Идентификатор пользователя в Telegram.</summary>
    public long TelegramId { get; set; }

    /// <summary>Username в Telegram (@name), может быть пустым.</summary>
    public string? Username { get; set; }

    /// <summary>Текущая роль учётной записи.</summary>
    public UserStatus Status { get; set; }

    /// <summary>Есть ли активный запрос на повышение роли.</summary>
    public UserPending Pending { get; set; }

    /// <summary>Дата и время регистрации (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Комментарий по пользователю для Админа</summary>
    public string? Description { get; set; }

    public BotUser()
    {
        Pending = UserPending.None;
    }

    public BotUser(long telegramId, string telegramUserName)
    {
        Id = Guid.NewGuid();
        Status = UserStatus.Guest;
        Pending = UserPending.None;
        Username = telegramUserName;
        TelegramId = telegramId;
        CreatedAt = DateTime.UtcNow;
    }
}
