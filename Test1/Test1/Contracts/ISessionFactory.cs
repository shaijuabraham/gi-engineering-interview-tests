using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Test1.Core;

namespace Test1.Contracts
{
    /// <inheritdoc />
    /// <summary>
    /// Builds and manages read/write and read only sessions to the underlying data store.
    /// </summary>
    public interface ISessionFactory : ISessionFactory<ISession, IReadOnlySession>
    {
    }

    /// <summary>
    /// Builds and manages read/write and read only sessions to the underlying data store.
    /// </summary>
    public interface ISessionFactory<out TSession, out TReadOnlySession>
    {
        /// <summary>
        /// Retrieve a read/write connection to the underlying relational data store.
        /// Do not manually dispose.
        /// </summary>
        TSession GetSession();

        /// <summary>
        /// Opens a new connection to the underlying relational data store. The created session must be disposed
        /// of after use.
        /// </summary>
        /// <param name="cancellationToken" />
        ValueTask<ISession> CreateNewSessionAsync(CancellationToken cancellationToken);
        
        /// <summary>
        /// Opens a new read/write database session and transaction pair. The underlying <see cref="IDbTransaction"/>
        /// will be started by this method and must be committed or rolled back.
        /// </summary>
        /// <param name="cancellationToken" />
        /// <returns>Read/write database session and transaction</returns>
        /// <remarks>Be sure to dispose <see cref="DapperDbContext"/>!</remarks>
        ValueTask<DapperDbContext> CreateContextAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Opens a new read/write database session and transaction pair. The underlying <see cref="IDbTransaction"/>
        /// will be started by this method and must be committed or rolled back.
        /// </summary>
        /// <param name="isolationLevel">One of the <see cref="T:System.Data.IsolationLevel" /> values.</param>
        /// <param name="cancellationToken" />
        /// <returns>Read/write database session and transaction</returns>
        /// <remarks>Be sure to dispose <see cref="DapperDbContext"/>!</remarks>
        ValueTask<DapperDbContext> CreateContextAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken);
    }
}