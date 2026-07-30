using System.Text.Json.Serialization;

namespace InfraBot.Entities;

/// <summary>
/// PowerShell-скрипт. Может возвращать результат в формате JSON.
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
    public string? Content { get; set; }

    /// <summary>Можно ли запускать скрипт.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Возвращаются ли данные в JSON.</summary>
    public bool ReturnData { get; set; }

    /// <summary>Требовать подтверждение пользователя перед запуском.</summary>
    public bool RequiresConfirmation { get; set; }

    /// <summary>Максимальное время выполнения в секундах.</summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>Дата и время создания скрипта (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Идентификатор Admin, создавшего скрипт (FK → BotUser).</summary>
    public Guid CreatedById { get; set; }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="user">ID пользователя</param>
    /// <param name="name">Имя скрипта</param>
    /// <param name="content">Тело скрипта</param>
    /// <param name="description">Описание</param>
    public Script(Guid user,
                  string name,
                  string? content,
                  string? description
    )
    {
        Id = Guid.NewGuid();
        IsEnabled = false;
        ReturnData = false;
        RequiresConfirmation = false;
        TimeoutSeconds = 120;
        CreatedAt = DateTime.UtcNow;
        CreatedById = user;
        Content = content;
        Name = name;
        Description = description;
    }
}
