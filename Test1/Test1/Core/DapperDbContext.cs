using System;
using System.Data;
using System.Threading.Tasks;
using Test1.Contracts;
using ISession = Test1.Contracts.ISession;

// ReSharper disable CheckNamespace

namespace Test1.Core
{
    public class DapperDbContext : IDisposable, IAsyncDisposable
    {
        private bool _disposed;

        public ISession Session { get; private set; }

        public IDbTransaction Transaction { get; private set; }

        ~DapperDbContext() => Dispose(false);

        public DapperDbContext(ISession session, IDbTransaction transaction)
        {
            Session = session;
            Transaction = transaction;
        }

        /// <summary>
        /// Start a new transaction. Any existing transaction will rollback if not specifically committed.
        /// </summary>
        public void BeginTransaction()
        {
            Transaction?.Dispose();
            Transaction = null;

            Transaction = Session.BeginTransaction();
        }

        public void Commit()
        {
            Transaction.Commit();
        }

        public void Rollback()
        {
            Transaction.Rollback();
        }

        /// <inheritdoc />
        /// <remarks>https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose</remarks>
        public void Dispose()
        {
            if (_disposed)
                return;

            Dispose(true);
            GC.SuppressFinalize(this);

            _disposed = true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                Session?.Dispose();
                Session = null;

                Transaction?.Dispose();
                Transaction = null;
            }

            _disposed = true;
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            if (Session != null)
            {
                await CastAndDispose(Session);
                Session = null;
            }

            if (Transaction != null)
            {
                await CastAndDispose(Transaction);
                Transaction = null;
            }

            _disposed = true;

            return;

            static async ValueTask CastAndDispose(IDisposable resource)
            {
                if (resource is IAsyncDisposable resourceAsyncDisposable)
                {
                    await resourceAsyncDisposable.DisposeAsync();
                }
                else
                {
                    resource.Dispose();
                }
            }
        }
    }
}