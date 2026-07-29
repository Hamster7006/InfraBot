using System;
using System.Security;

namespace InfraBot.Entities;

/// <summary>
/// Сервер в домене для удалённого управления через WinRM.
/// </summary>
public class Server
{
    /// <summary>
    /// идентификатор сервера
    /// </summary>
    public Guid ServerId { get; set; }
    /// <summary>
    /// Краткое имя
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Hostname или FQDN сервера в домене
    /// </summary>
    public string Hostname { get; set; }
    /// <summary>
    /// Описание сервера
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Пользователь кто зарегистрировал сервер
    /// </summary>
    public BotUser User { get; set; }
    /// <summary>
    /// Доступен ли сервер для операций
    /// </summary>
    public bool IsEnabled { get; set; }
    /// <summary>
    /// Порт WinRM: 5985 (HTTP) или 5986 (HTTPS)
    /// </summary>
    public int WinRmPort { get; set; }
    /// <summary>
    /// Уз у которой есть права
    /// </summary>
    public string? Samaccuntname { get; set; }
    /// <summary>
    /// Пароль от уз
    /// </summary>
    private SecureString? Password { get; set; }
    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="name"></param>
    /// <param name="hostname"></param>
    /// <param name="description"></param>
    public Server(BotUser user, string name, string hostname, string? description)
    {
         ServerId = Guid.NewGuid();
        User = user;
        Name = name;
        Hostname = hostname;
        Description = !string.IsNullOrWhiteSpace(description) ? description : string.Empty;
        WinRmPort = 5986;
        IsEnabled = false;
    }
}
