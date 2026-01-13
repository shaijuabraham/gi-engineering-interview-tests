using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Test1.Contracts
{
    /// <summary>
    /// Represents a read only connection to the underlying ADO.NET data source.
    /// </summary>
    /// <remarks>This is likely a connection to a read replica</remarks>
    public interface IReadOnlySession : IDisposable
    {
        /// <summary>
        /// True if the underlying connection is read only.
        /// </summary>
        bool ReadOnly { get; }

        /// <summary>
        /// True if the underlying transaction was rolled back
        /// </summary>
        bool RolledBack { get; }

        /// <summary>
        /// ADO.NET connection to the underlying data store.
        /// </summary>
        IDbConnection Connection { get; }

        /// <summary>
        /// Current transaction, if any.
        /// </summary>
        IDbTransaction Transaction { get; }

        /// <summary>
        /// Begins a database transaction.
        /// </summary>
        /// <param name="isolationLevel">One of the <see cref="T:System.Data.IsolationLevel" /> values.</param>
        /// <returns>An object representing the new transaction.</returns>
        IDbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Unspecified);

        /// <summary>
        /// Commit the current transaction and optionally start a new one for
        /// future use of the current session/connection
        /// </summary>
        /// <param name="startNewTransaction"></param>
        void CommitTransaction(bool startNewTransaction = true);

        /// <summary>
        /// Execute a query asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of results to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        /// <returns>
        /// A sequence of data of <typeparamref name="T"/>; if a basic type (int, string, etc) is queried then the data from the first column in assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null);

        /// <summary>
        /// Perform a asynchronous multi-mapping query with 2 input types.
        /// This returns a single type, combined from the raw types via <paramref name="map"/>.
        /// </summary>
        /// <typeparam name="TFirst">The first type in the recordset.</typeparam>
        /// <typeparam name="TSecond">The second type in the recordset.</typeparam>
        /// <typeparam name="TReturn">The combined type to return.</typeparam>
        /// <param name="sql">The SQL to execute for this query.</param>
        /// <param name="map">The function to map row types to the return type.</param>
        /// <param name="param">The parameters to use for this query.</param>
        /// <param name="splitOn">The field we should split and read the second object from (default: "Id").</param>
        /// <returns>An enumerable of <typeparamref name="TReturn"/>.</returns>
        Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(
            string sql,
            Func<TFirst, TSecond, TReturn> map,
            object param = null,
            string splitOn = "Id");

        /// <summary>
        /// Execute a single-row query asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of result to return.</typeparam>
        /// <param name="sql">The SQL to execute for the query.</param>
        /// <param name="param">The parameters to pass, if any.</param>
        /// <param name="transaction">The transaction to use, if any.</param>
        Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, IDbTransaction transaction = null);

        /// <summary>
        /// Rollback any active transaction.
        /// </summary>
        void Rollback();
    }
}