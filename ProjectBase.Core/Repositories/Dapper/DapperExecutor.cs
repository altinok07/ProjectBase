using global::Dapper;
using ProjectBase.Core.Results;
using System.Data;

namespace ProjectBase.Core.Repositories.Dapper;

/// <summary>
/// Default implementation of <see cref="IDapperExecutor"/>.
/// </summary>
public sealed class DapperExecutor(IDbConnectionProvider db) : IDapperExecutor
{
    private IDbConnection Connection => db.Connection;
    private IDbTransaction? Transaction => db.Transaction;

    public async Task<Result<IEnumerable<T>>> QueryAsync<T>(
        string sql,
        object? param = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cmd = new CommandDefinition(
                commandText: sql,
                parameters: param,
                transaction: Transaction,
                commandType: commandType,
                cancellationToken: cancellationToken);

            var rows = await Connection.QueryAsync<T>(cmd);
            return Result<IEnumerable<T>>.Success(ResultType.Success, rows, "Success");
        }
        catch (Exception ex)
        {
            return DapperRepositoryExtensions.ExceptionError<IEnumerable<T>>(ex, "QueryFailed");
        }
    }

    public async Task<Result<T?>> QuerySingleOrDefaultAsync<T>(
        string sql,
        object? param = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cmd = new CommandDefinition(
                commandText: sql,
                parameters: param,
                transaction: Transaction,
                commandType: commandType,
                cancellationToken: cancellationToken);

            var row = await Connection.QuerySingleOrDefaultAsync<T>(cmd);
            return Result<T?>.Success(ResultType.Success, row, "Success");
        }
        catch (Exception ex)
        {
            return DapperRepositoryExtensions.ExceptionError<T?>(ex, "QueryFailed");
        }
    }

    public async Task<Result<int>> ExecuteAsync(
        string sql,
        object? param = null,
        CommandType? commandType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cmd = new CommandDefinition(
                commandText: sql,
                parameters: param,
                transaction: Transaction,
                commandType: commandType,
                cancellationToken: cancellationToken);

            var affected = await Connection.ExecuteAsync(cmd);
            return Result<int>.Success(ResultType.Success, affected, "Success");
        }
        catch (Exception ex)
        {
            return DapperRepositoryExtensions.ExceptionError<int>(ex, "ExecuteFailed");
        }
    }
}


