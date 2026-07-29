using InfraBot.Entities.Enums;

namespace InfraBot.Entities;

/// <summary>
/// Пользователь Telegram-бота.
/// </summary>
public class BotUser
{
    /// <summary>
    /// Уникальный идентификатор в базе данных.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Идентификатор пользователя в Telegram.
    /// </summary>
    public long TelegramId { get; set; }
    /// <summary>
    /// Username в Telegram (@name), может быть пустым.
    /// </summary>
    public string? Username { get; set; }
    /// <summary>
    /// Статус учётной записи
    /// </summary>
    public UserStatus Status { get; set; }
    /// <summary>
    /// Дата и время регистрации (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// Серверы, к которым у пользователя есть доступ.
    /// </summary>
    public List<ServerAndScripts> ServerAccesses { get; set; }
    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="telegramId"></param>
    /// <param name="telegramUserName"></param>
    public BotUser(long telegramId, string telegramUserName)
    {
        Id = Guid.NewGuid();
        Status = UserStatus.Guest;
        Username = telegramUserName;
        TelegramId = telegramId;
        CreatedAt = DateTime.UtcNow;
        ServerAccesses = new List<ServerAndScripts>();
    }
}
