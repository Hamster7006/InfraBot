namespace InfraBot.Entities.Enums;

/// <summary>
/// Статус выполнения задачи (запуска скрипта).
/// </summary>
public enum JobRunStatus
{
    /// <summary>Задача в очереди, ожидает выполнения.</summary>
    Queued = 0,

    /// <summary>Скрипт выполняется на сервере.</summary>
    Running = 1,

    /// <summary>Скрипт завершился успешно, JSON-результат получен.</summary>
    Success = 2,

    /// <summary>Ошибка выполнения (WinRM, таймаут, некорректный JSON и т.д.).</summary>
    Failed = 3,

    /// <summary>Задача отменена до или во время выполнения.</summary>
    Cancelled = 4
}
