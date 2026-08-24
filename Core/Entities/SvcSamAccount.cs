namespace InfraBot.Entities;

/// <summary>
/// Учётная запись домена для WinRM-подключения к серверу.
/// Server N:1 SvcSamAccount.
/// </summary>
public class SvcSamAccount
{
    /// <summary>Уникальный идентификатор записи.</summary>
    public Guid Id { get; set; }

    /// <summary>Имя учётной записи в домене (sAMAccountName).</summary>
    public string SamAccountName { get; set; }

    /// <summary>Пароль</summary>
    public string Password { get; set; }

    public SvcSamAccount()
    {
        SamAccountName = string.Empty;
        Password = string.Empty;
    }

    public SvcSamAccount(string samAccountName, string password)
    {
        Id = Guid.NewGuid();
        SamAccountName = samAccountName;
        Password = password;
    }
}
