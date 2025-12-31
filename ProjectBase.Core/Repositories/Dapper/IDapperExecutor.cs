using ProjectBase.Core.Results;
using System.Data;

namespace ProjectBase.Core.Repositories.Dapper;

/// <summary>
/// Generic Dapper executor you can inject directly into handlers/services.
/// Uses <see cref="IDbConnectionProvider"/> so it can share EF Core connection/transaction.
/// </summary>
public interface IDapperExecutor
{
    Task<Result<IEnumerable<T>>> QueryAsync<T>(
        string sql,
        object? param = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default);

    Task<Result<T?>> QuerySingleOrDefaultAsync<T>(
        string sql,
        object? param = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default);

    Task<Result<int>> ExecuteAsync(
        string sql,
        object? param = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default);
}


