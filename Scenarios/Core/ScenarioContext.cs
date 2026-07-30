namespace InfraBot.Scenarios.Core;

public enum ScenarioType
{
    None,
    Server,
    Script,
    SvcSamAccount,
    ServerAccess,
    JobRun,
}

/// <summary>Операция внутри сценария (хранится в <see cref="ScenarioContext.Data"/>).</summary>
public enum ScenarioAction
{
    Create,
    Update,
    Delete,
    Grant,
    Revoke,
    SetScriptRequirement,
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
