using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data.Common;

namespace ProjectBase.Core.Repositories.Dapper;

/// Exposes EF Core's underlying connection and current transaction for Dapper usage.
/// This enables Dapper queries to participate in the same transaction started via EF Core.
/// </summary>
public sealed class EfCoreDbConnectionProvider<TContext>(TContext context) : IDbConnectionProvider
    where TContext : DbContext
{
    public DbConnection Connection => context.Database.GetDbConnection();

    public DbTransaction? Transaction => context.Database.CurrentTransaction?.GetDbTransaction();
}
