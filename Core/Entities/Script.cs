namespace InfraBot.Entities;

/// <summary>
/// PowerShell-скрипт. Может возвращать результат в формате JSON.
/// Имя уникально в рамках глобального каталога.
/// </summary>
public class Script
{
    /// <summary>Уникальный идентификатор скрипта.</summary>
    public Guid Id { get; set; }

    /// <summary>Уникальное имя скрипта для выбора в боте.</summary>
    public string Name { get; set; }

    /// <summary>Описание скрипта.</summary>
    public string? Description { get; set; }

    /// <summary>Текст PowerShell-скрипта.</summary>
    public string Content { get; set; }

    /// <summary>Возвращаются ли данные в JSON.</summary>
    public bool ReturnData { get; set; }

    /// <summary>Максимальное время выполнения в секундах.</summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>Дата и время создания скрипта (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Идентификатор Admin, создавшего скрипт (FK → BotUser).</summary>
    public Guid CreatedById { get; set; }

    public Script()
    {
        Name = string.Empty;
        Content = string.Empty;
    }

    public Script(Guid createdById, string name, string content, string? description)
    {
        Id = Guid.NewGuid();
        ReturnData = false;
        TimeoutSeconds = 120;
        CreatedAt = DateTime.UtcNow;
        CreatedById = createdById;
        Content = content;
        Name = name;
        Description = description;
    }
}
