using Npgsql;

namespace MEDICSYS.Api.Data;

public sealed class AppDbDataSource
{
    public AppDbDataSource(NpgsqlDataSource dataSource) => DataSource = dataSource;
    public NpgsqlDataSource DataSource { get; }
}

public sealed class AcademicDbDataSource
{
    public AcademicDbDataSource(NpgsqlDataSource dataSource) => DataSource = dataSource;
    public NpgsqlDataSource DataSource { get; }
}

public sealed class OdontologoDbDataSource
{
    public OdontologoDbDataSource(NpgsqlDataSource dataSource) => DataSource = dataSource;
    public NpgsqlDataSource DataSource { get; }
}
