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

    /// <summary>IP-адрес сервера.</summary>
    public string IpAddress { get; set; }

    /// <summary>Описание сервера.</summary>
    public string? Description { get; set; }

    /// <summary>Идентификатор пользователя, зарегистрировавшего сервер (FK → BotUser).</summary>
    public Guid RegisteredByUserId { get; set; }

    /// <summary>Порт WinRM: 5985 (HTTP) или 5986 (HTTPS).</summary>
    public int WinRmPort { get; set; }

    /// <summary>Идентификатор учётной записи службы для WinRM (FK → SvcSamAccount).</summary>
    public Guid SvcSamAccountId { get; set; }

    /// <summary>Скрипты, привязанные к серверу.</summary>
    public List<Guid> ScriptRequirements { get; set; }

    /// <summary>Пользователи, которым выдан доступ к серверу (FK → BotUser).</summary>
    public List<Guid> GrantedUserIds { get; set; }

    public Server()
    {
        ServerName = string.Empty;
        IpAddress = string.Empty;
        ScriptRequirements = [];
        GrantedUserIds = [];
    }

    public Server(string name, Guid user)
    {
        Id = Guid.NewGuid();
        RegisteredByUserId = user;
        ServerName = name;
        WinRmPort = 5986;
        ScriptRequirements = [];
        GrantedUserIds = [];
    }
}
