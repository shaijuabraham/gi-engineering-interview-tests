using System.Data;
using System.Threading.Tasks;

namespace Test1.Contracts
{
    /// <inheritdoc />
    /// <summary>
    /// Represents a read/write connection to the underlying ADO.NET data source.
    /// </summary>
    public interface ISession : IReadOnlySession
    {
        /// <summary>
        /// Execute a command asynchronously.
        /// </summary>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="transaction">The transaction to use for this query.</param>
        /// <returns>The number of rows affected.</returns>
        Task<int> ExecuteAsync(string sql, object param = null, IDbTransaction transaction = null);

        /// <summary>
        /// Execute parameterized SQL that selects a single value.
        /// </summary>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <param name="sql">The SQL to execute.</param>
        /// <param name="param">The parameters to use for this command.</param>
        /// <returns>The first cell returned, as <typeparamref name="T"/>.</returns>
        Task<T> ExecuteScalarAsync<T>(string sql, object param = null, IDbTransaction transaction = null);
    }
}