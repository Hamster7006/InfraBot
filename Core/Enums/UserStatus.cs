namespace InfraBot.Enums;

/// <summary>
/// Роль пользователя Telegram-бота (без состояний ожидания).
/// </summary>
public enum UserStatus
{
    /// <summary>Полный доступ: пользователи, серверы, скрипты, выдача прав.</summary>
    Admin = 0,

    /// <summary>Зарегистрирован, базовый доступ.</summary>
    Guest = 1,

    /// <summary>Может запускать разрешённые скрипты и смотреть доступные серверы.</summary>
    Operator = 2,
}
