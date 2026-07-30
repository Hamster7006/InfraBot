namespace InfraBot.Enums;

/// <summary>
/// Роль пользователя Telegram-бота (без состояний ожидания).
/// </summary>
public enum UserStatus
{
    /// <summary>Заблокирован Admin.</summary>
    Blocked = 0,

    /// <summary>Зарегистрирован, базовый доступ.</summary>
    Guest = 1,

    /// <summary>Может запускать разрешённые скрипты и смотреть доступные серверы.</summary>
    Operator = 2,

    /// <summary>Старший оператор: расширенные права на запуск скриптов и управление доступом к серверам.</summary>
    MainOperator = 3,

    /// <summary>Полный доступ: пользователи, серверы, скрипты, выдача прав.</summary>
    Admin = 4
}
