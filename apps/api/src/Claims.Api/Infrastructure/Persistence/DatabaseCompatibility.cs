using Microsoft.EntityFrameworkCore;

namespace Claims.Api.Infrastructure.Persistence;

public static class DatabaseCompatibility
{
    public static Task EnsureLevel170Async(
        ClaimsDbContext dbContext,
        CancellationToken cancellationToken = default) =>
        dbContext.Database.ExecuteSqlRawAsync(
            """
            IF (SELECT [compatibility_level] FROM [sys].[databases] WHERE [name] = DB_NAME()) <> 170
            BEGIN
                DECLARE @databaseName sysname = DB_NAME();
                DECLARE @statement nvarchar(max) =
                    N'ALTER DATABASE ' + QUOTENAME(@databaseName) + N' SET COMPATIBILITY_LEVEL = 170';
                EXEC sys.sp_executesql @statement;
            END
            """,
            cancellationToken);
}
