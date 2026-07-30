using InfraBot.Enums;

namespace InfraBot.Entities;

/// <summary>
/// Сервер в домене для удалённого управления через WinRM.
/// </summary>
public class Server
{
    /// <summary>Идентификатор сервера.</summary>
    public Guid Id { get; set; }

    /// <summary>Краткое имя.</summary>
    public string ServerName { get; set; }

    /// <summary>Hostname или FQDN сервера в домене.</summary>
    public string ServerFQDN { get; set; }

    /// <summary>Описание сервера.</summary>
    public string? Description { get; set; }

    /// <summary>Идентификатор пользователя, зарегистрировавшего сервер (FK → BotUser).</summary>
    public Guid RegisteredByUserId { get; set; }

    /// <summary>Доступен ли сервер для операций.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Порт WinRM: 5985 (HTTP) или 5986 (HTTPS).</summary>
    public int WinRmPort { get; set; }

    /// <summary>Идентификатор учётной записи службы для WinRM (FK → SvcSamAccount).</summary>
    public Guid? SvcSamAccountId { get; set; }

    /// <summary>
    /// Скрипты, привязанные к серверу: ID скрипта → минимальная роль для запуска.
    /// </summary>
    public Dictionary<Guid, UserStatus> ScriptRequirements { get; set; }

    /// <summary>Пользователи, которым выдан доступ к серверу (FK → BotUser).</summary>
    public List<Guid> GrantedUserIds { get; set; }

    public Server()
    {
        ServerName = string.Empty;
        ServerFQDN = string.Empty;
        ScriptRequirements = [];
        GrantedUserIds = [];
    }

    public Server(string name, string hostname, Guid user)
    {
        Id = Guid.NewGuid();
        RegisteredByUserId = user;
        ServerName = name;
        ServerFQDN = hostname;
        WinRmPort = 5986;
        IsEnabled = false;
        ScriptRequirements = [];
        GrantedUserIds = [];
    }
}
