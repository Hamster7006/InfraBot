namespace InfraBot.Scenarios.Core;

public enum ScenarioType
{
    None,
    /// <summary>Добавление сервера (/addserver).</summary>
    Server,
    /// <summary>Удаление сервера из карточки (двойное подтверждение).</summary>
    DeleteServer,
    /// <summary>Изменение атрибутов сервера из карточки.</summary>
    UpdateServer,
    Script,
    /// <summary>Изменение атрибутов скрипта из карточки.</summary>
    UpdateScript,
    /// <summary>Удаление скрипта из карточки (двойное подтверждение).</summary>
    DeleteScript,
    /// <summary>Добавление WinRM УЗ (/addsvcaccount).</summary>
    SvcSamAccount,
    /// <summary>Смена пароля WinRM УЗ из карточки.</summary>
    UpdateSvcSamAccount,
    /// <summary>Удаление WinRM УЗ из карточки.</summary>
    DeleteSvcSamAccount,
    /// <summary>Запуск скрипта на сервере (JobRun).</summary>
    RunJob,
    /// <summary>Управление пользователями (/usercontrol).</summary>
    User,
}

/// <summary>Операция внутри сценария (хранится в <see cref="ScenarioContext.Data"/>).</summary>
public enum ScenarioAction
{
    Create,
    Update,
    Delete,
    Grant,
    Revoke,
}

public enum ScenarioResult
{
    Transition,
    Completed
}

public class ScenarioContext
{
    internal ScenarioType currentScenario { get; set; }
    internal string? CurrentStep { get; set; }
    internal Dictionary<string, object> Data { get; } = new();

    public ScenarioContext(ScenarioType scenario)
    {
        currentScenario = scenario;
    }
}
