using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Test1.Contracts;
using ISession = Test1.Contracts.ISession;

// ReSharper disable ClassNeverInstantiated.Global

namespace Test1.Core
{
    /// <inheritdoc cref="ISession" />
    /// <summary>
    /// Represents a connection to the underlying data store that reuses the DbConnection and
    /// DbTransaction within the current Dependency Injection scope (typically the duration
    /// of the current web request).
    /// </summary>
    public class ScopedSession : ISession
    {
        /// <summary>
        /// This is for tracking objects with the debugger.
        /// </summary>
        private readonly Guid _trackingGuid;

        /// <summary>
        /// 0 for false, 1 for true.
        /// </summary>
        private int _disposed;

        /// <inheritdoc />
        public bool ReadOnly { get; private set; }

        /// <inheritdoc />
        public bool RolledBack { get; private set; }

        /// <inheritdoc />
        public IDbConnection Connection { get; private set; }

        /// <inheritdoc />
        public IDbTransaction Transaction { get; private set; }

        public ScopedSession(IDbConnection connection, bool startTransaction = true)
        {
            _trackingGuid = Guid.NewGuid();

            Connection = connection;

            if (startTransaction)
                Transaction = connection.BeginTransaction();
        }

        /// <inheritdoc />
        public IDbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Unspecified)
        {
            if (Transaction != null)
                throw new Exception("There is already a transaction associated with this session!");

            return Connection.BeginTransaction(isolationLevel);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null)
            => await Connection.QueryAsync<T>(sql, param, transaction ?? Transaction)
                .ConfigureAwait(false);

        /// <inheritdoc />
        public async Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, object param = null, string splitOn = "Id")
            => await Connection.QueryAsync(sql, map, param, Transaction, splitOn: splitOn)
                .ConfigureAwait(false);

        /// <inheritdoc />
        public async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, IDbTransaction transaction = null)
            => await Connection.QueryFirstOrDefaultAsync<T>(sql, param, transaction ?? Transaction)
                .ConfigureAwait(false);

        /// <inheritdoc />
        public async Task<int> ExecuteAsync(string sql, object param = null, IDbTransaction transaction = null)
            => await Connection.ExecuteAsync(sql, param, transaction ?? Transaction)
                .ConfigureAwait(false);

        /// <inheritdoc />
        public async Task<T> ExecuteScalarAsync<T>(string sql, object param = null, IDbTransaction transaction = null)
            => await Connection.ExecuteScalarAsync<T>(sql, param, transaction ?? Transaction)
                .ConfigureAwait(false);

        /// <inheritdoc />
        public void CommitTransaction(bool startNewTransaction = true)
        {
            if (!RolledBack)
            {
                Transaction?.Commit();
            }
            else
            {
                Transaction?.Rollback();
            }

            if (startNewTransaction)
            {
                RolledBack = false;
                Transaction = Connection.BeginTransaction();
            }
            else
            {
                // Nullify the transaction to avoid trying to commit it again
                Transaction = null;
            }
        }

        /// <summary>
        /// <para>
        ///   Replace the current session's underlying database connection and transaction
        ///   with the provided replacement's current database connection and transaction.
        /// </para>
        /// <para>
        ///   This method must dispose of and close the original session's connection and
        ///   transaction to avoid leaking connections.
        /// </para>
        /// </summary>
        public void Replace(ISession replacement)
        {
            // First close the session's current transaction and connection
            if (!RolledBack)
            {
                Transaction?.Commit();
            }
            else
            {
                Transaction?.Dispose();
            }

            Connection?.Close();
            Connection?.Dispose();

            // Replace the session's underlying connection and transaction
            Connection = replacement.Connection;
            Transaction = replacement.Transaction;
            ReadOnly = replacement.ReadOnly;
            RolledBack = replacement.RolledBack;
        }

        /// <inheritdoc />
        public void Rollback()
        {
            if (RolledBack)
                return;

            try
            {
                Transaction?.Rollback();
            }
            catch
            {
                // ignore
            }

            RolledBack = true;
        }

        /// <summary>
        /// Display the <see cref="_trackingGuid"/> in the debugger.
        /// </summary>
        public override string ToString()
        {
            return _trackingGuid.ToString();
        }

        public void Dispose()
        {
            // Ensure we're only executed once
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            if (!RolledBack)
            {
                Transaction?.Commit();
            }
            else
            {
                Transaction?.Dispose();
            }

            Connection?.Close();
            Connection?.Dispose();
        }
    }
}