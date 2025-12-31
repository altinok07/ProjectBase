using System.Data.Common;

namespace ProjectBase.Core.Repositories.Dapper;

/// <summary>
/// Provides an existing database connection (and optional transaction) for Dapper operations.
/// The provider owns the connection lifetime; repositories should NOT dispose the connection.
/// </summary>
public interface IDbConnectionProvider
{
    DbConnection Connection { get; }
    DbTransaction? Transaction { get; }
}


