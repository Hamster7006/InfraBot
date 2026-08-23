using LinqToDB;
using LinqToDB.Data;
using InfraBot.Core.DataAccess.Models;

namespace InfraBot.Infrastracture.DataAccess;

internal sealed class InfraDataContext : DataConnection
{
    public InfraDataContext(string connectionString)
        : base(new DataOptions().UseConnectionString(ProviderName.PostgreSQL, connectionString))
    {
    }

    public ITable<BotUserModel> BotUsers => this.GetTable<BotUserModel>();
    public ITable<SvcSamAccountModel> SvcSamAccounts => this.GetTable<SvcSamAccountModel>();
    public ITable<ScriptModel> Scripts => this.GetTable<ScriptModel>();
    public ITable<ServerModel> Servers => this.GetTable<ServerModel>();
    public ITable<JobRunModel> JobRuns => this.GetTable<JobRunModel>();
    public ITable<ServerScriptRequirementModel> ServerScriptRequirements =>
        this.GetTable<ServerScriptRequirementModel>();
    public ITable<ServerGrantedUserModel> ServerGrantedUsers =>
        this.GetTable<ServerGrantedUserModel>();
}
