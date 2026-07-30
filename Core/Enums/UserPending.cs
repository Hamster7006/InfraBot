namespace InfraBot.Enums;

/// <summary>
/// Статус запроса на повышение роли (ожидает решения Admin).
/// </summary>
public enum UserPending
{
    /// <summary>Нет активного запроса.</summary>
    None = 0,

    /// <summary>Запрос на повышение отправлен, ожидает одобрения Admin.</summary>
    Pending = 1
}
