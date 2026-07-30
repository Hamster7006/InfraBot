using System.Security;
using System.Text.Json.Serialization;

namespace InfraBot.Entities;

/// <summary>
/// Учётная запись домена для WinRM-подключения к серверу.
/// Server 1:1 ServerSamAccountUser.
/// </summary>
internal class SvcSamAccount
{
    /// <summary>Уникальный идентификатор записи.</summary>
    internal Guid Id { get; set; }

    /// <summary>Имя учётной записи в домене (sAMAccountName).</summary>
    internal string SamAccountName { get; set; }

    /// <summary>Пароль.</summary>
    internal string Password { get; set; }
    internal SvcSamAccount(string samAccountName, string password)
    {
        Id = Guid.NewGuid();
        SamAccountName = samAccountName;
        Password = password;
    }
}
