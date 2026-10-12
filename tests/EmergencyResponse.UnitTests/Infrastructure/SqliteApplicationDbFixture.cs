using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.UnitTests.Infrastructure;

internal sealed class SqliteApplicationDbFixture : IAsyncDisposable
{
    private SqliteApplicationDbFixture(
        SqliteConnection connection,
        ApplicationDbContext context)
    {
        Connection = connection;
        Context = context;
    }

    public SqliteConnection Connection { get; }

    public ApplicationDbContext Context { get; }

    public static async Task<SqliteApplicationDbFixture> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return new SqliteApplicationDbFixture(connection, context);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await Connection.DisposeAsync();
    }
}
