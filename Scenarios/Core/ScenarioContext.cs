using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfraBot.Scenarios.Core
{
    public enum ScenarioType
    {
        None,
        #region Server
        AddServer,
        UpdateServerData,
        DeleteServer,
        #endregion
        #region Script
        AddScript,
        UpdateScript,
        #endregion
        #region Svc
        AddSvc,
        UpdateSvc,
        #endregion
        #region JobRun
        CreateJob,
        #endregion
        #region ServerScripts
        CreateLink,
        AddScriptToLink,
        DeleteScriptFromLink,
        #endregion
    }
    public enum ScenarioResult
    {
        Transition, //- Переход к следующему шагу. Сообщение обработано, но сценарий еще не завершен
        Completed   // - Сценарий завершен
    }
    public class ScenarioContext
    {
        internal ScenarioType currentScenario { get; set; }
        internal string? CurrentStep { get; set; }
        internal Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public ScenarioContext(ScenarioType scenario)
        {
            currentScenario = scenario;
        }
    }
}
