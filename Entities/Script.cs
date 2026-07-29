namespace InfraBot.Entities;

/// <summary>
/// PowerShell-скрипт, хранящийся в PostgreSQL.
/// Скрипт может возвращать результат в формате JSON.
/// </summary>
public class Script
{
    /// <summary>
    /// Уникальный идентификатор скрипта.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Уникальное имя скрипта для выбора в боте.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Описание скрипта
    /// </summary>
    public string? Description { get; set; } //
    /// <summary>
    /// Текст PowerShell-скрипта
    /// </summary>
    public string Content { get; set; } = string.Empty; //
    /// <summary>
    /// Можно ли запускать скрипт (false = временно отключён)
    /// </summary>
    public bool IsEnabled { get; set; } = true; //
    /// <summary>
    /// Возвращаются ли данныйе в JSON (false = нет)
    /// </summary>
    public bool ReturnData { get; set; } = false; //
    /// <summary>
    /// Требовать подтверждение пользователя перед запуском.
    /// </summary>
    public bool RequiresConfirmation { get; set; }
    /// <summary>
    /// Максимальное время выполнения в секундах.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;
    /// <summary>
    /// Дата и время создания скрипта (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Admin, создавший скрипт.
    /// </summary>
    public BotUser CreatedBy { get; set; } = null!;
    /// <summary>
    /// История запусков этого скрипта.
    /// </summary>
    public List<JobRun> JobRuns { get; set; } = [];
}
