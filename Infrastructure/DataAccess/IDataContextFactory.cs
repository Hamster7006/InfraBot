using LinqToDB.Data;

namespace InfraBot.Infrastructure.DataAccess;

internal interface IDataContextFactory<TDataContext>
    where TDataContext : DataConnection
{
    TDataContext CreateDataContext();
}
