using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Test1.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ISession = Test1.Contracts.ISession;

// ReSharper disable ClassNeverInstantiated.Global

namespace Test1.Core
{
    /// <summary>
    /// A scoped MySqlSession factory that handles creating and providing both
    /// read/write and read only connections to the database. Contains logic to
    /// help prevent accidental state mismatch between read/write and read only connections
    /// during a single request scope.
    /// </summary>
    internal class SqliteSessionFactory : ISessionFactory
    {
        private const string _dbPath = @"Test1\bin\Debug\net10.0\data\Test1.db";

        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SqliteSessionFactory> _logger;

        /// <summary>
        /// A scoped connection to the database that will be disposed of automatically
        /// at the end of a request's scope. Will either be a R/W or RO connection
        /// depending on the factory's <see cref="_isReadOnlyMode"/> flag.
        /// </summary>
        private ScopedSession _scopedSession;

        /// <summary>
        /// Flag that tracks the current state of the factory's stored internal scoped session.
        /// Used to ensure any requested readonly sessions include any open R/W transactions to
        /// avoid potential inconsistencies between <see cref="ISession"/> and <see cref="IReadOnlySession"/>
        /// </summary>
        private bool _isReadOnlyMode = true;

        public SqliteSessionFactory(
            IWebHostEnvironment environment,
            ILogger<SqliteSessionFactory> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        /// <inheritdoc />
        public ISession GetSession()
        {
            if (_scopedSession != null)
            {
                // If a connection already exists that is not RO, re-use it
                if (!_isReadOnlyMode)
                    return _scopedSession;

                // We need to create a new R/W session
                var newSession = CreateNewSession(false);

                // Replace the factory's internal session with a R/W connection
                _scopedSession.Replace(newSession);

                // Keep track that the factory's session is no longer read only
                _isReadOnlyMode = false;

                // Return the now updated R/W session
                return _scopedSession;
            }

            // No session exists, so create a new one
            _scopedSession = (ScopedSession) CreateNewSession(false);

            // Keep track that the factory's session is no longer read only
            _isReadOnlyMode = false;

            return _scopedSession;
        }

        /// <inheritdoc />
        public ISession CreateNewSession(bool readOnly, bool startTransaction = true)
        {
            string connectionString = $"Data Source={_dbPath}";

            DbConnection connection = new SqliteConnection(connectionString);

            connection.Open();

            return new ScopedSession(connection, startTransaction);
        }

        /// <inheritdoc />
        public async ValueTask<ISession> CreateNewSessionAsync(CancellationToken cancellationToken)
        {
            string connectionString = $"Data Source={_dbPath}";

            DbConnection connection = new SqliteConnection(connectionString);

            var cwd = System.IO.Directory.GetCurrentDirectory();

            await connection.OpenAsync(cancellationToken)
                .ConfigureAwait(false);

            return new ScopedSession(connection, startTransaction: false);
        }

        /// <inheritdoc />
        public ValueTask<DapperDbContext> CreateContextAsync(CancellationToken cancellationToken)
            => CreateContextAsync(IsolationLevel.Unspecified, cancellationToken);

        /// <inheritdoc />
        public async ValueTask<DapperDbContext> CreateContextAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
        {
            var session = await CreateNewSessionAsync(cancellationToken)
                .ConfigureAwait(false);

            var txn = session.BeginTransaction(isolationLevel);

            return new DapperDbContext(session, txn);
        }
    }
}