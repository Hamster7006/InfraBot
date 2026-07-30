namespace InfraBot.Enums;

/// <summary>
/// Статус учётной записи пользователя.
/// </summary>
public enum UserStatus
{
    /// <summary>Заблокирован Admin.</summary>
    Blocked = 0,
    /// <summary>Зарегистрирован, но ещё не одобрен Admin.</summary>
    Guest = 1,
    /// <summary>Ожидает рассмотрения Admin.</summary>
    Pending = 2,
    /// <summary>Может запускать разрешённые скрипты и смотреть доступные серверы.</summary>
    Operator = 4,
    /// <summary>Полный доступ: пользователи, серверы, скрипты, выдача прав.</summary>
    Admin = 8    
}
