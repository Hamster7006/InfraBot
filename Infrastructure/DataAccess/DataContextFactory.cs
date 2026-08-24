namespace InfraBot.Infrastructure.DataAccess;

internal sealed class DataContextFactory : IDataContextFactory<InfraDataContext>
{
    private readonly string _connectionString;

    public DataContextFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Строка подключения к БД не указана.");

        _connectionString = connectionString;
    }

    public InfraDataContext CreateDataContext() => new(_connectionString);
}
