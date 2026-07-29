using InfraBot.Entities.Enums;

namespace InfraBot.Entities;

/// <summary>
/// Связь роли и скрипта — какие скрипты доступны Operator (или другой роли).
/// </summary>
public class ServerAndScripts
{
    /// <summary>
    /// Уникальный идентификатор записи.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Сервер
    /// </summary>
    public Server Server { get; set; }
    /// <summary>
    /// Скрипты, доступные для сервера.
    /// </summary>
    public List<Script> ListScript { get; set; }

    public ServerAndScripts(Server server)
    {
        Id = Guid.NewGuid();
        Server= server;
        ListScript = new List<Script>();
    }
}
