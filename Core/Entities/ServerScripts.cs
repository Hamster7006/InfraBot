using System.Text.Json.Serialization;

namespace InfraBot.Entities;

/// <summary>
/// Связь пользователя с сервером и его скриптами.
/// BotUser 1:N ServerAndScript, ServerAndScript 1:1 Server, ServerAndScript 1:N Script.
/// </summary>
public class ServerScripts
{
    /// <summary>Уникальный идентификатор записи.</summary>
    public Guid Id { get; set; }

    /// <summary>Идентификатор сервера (FK → Server).</summary>
    public Guid ServerId { get; set; }

    /// <summary>Скрипты, доступные в этой связке.</summary>
    public List<Guid> ScriptsIds { get; set; }

    public ServerScripts(Guid serverId)
    {
        Id = Guid.NewGuid();
        ScriptsIds = new List<Guid>();
        ServerId = serverId;
   }
}
