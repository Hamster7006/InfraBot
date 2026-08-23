using LinqToDB.Data;

namespace InfraBot.Infrastracture.DataAccess;

internal interface IDataContextFactory<TDataContext>
    where TDataContext : DataConnection
{
    TDataContext CreateDataContext();
}
