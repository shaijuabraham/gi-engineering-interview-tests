using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Test1.Contracts;
using Test1.Core;
using Test1.Models;


namespace Test1.Controllers
{
    [ApiController]// indicates that this class is an API controller
    // sets the base route for the controller
    // So the AccountsController will respond to respond to (api/accounts)"
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase //base class for API controllers only not views
    {
        // Session factory for database connections
        // It is used for managing connection and transactions to the database
        // no need to create connectionstring manually
        private readonly ISessionFactory _sessionFactory;
        // The session factory used to get database sessions. 
        // Each session is connected to the database file (test1.db),
        public AccountsController(ISessionFactory sessionFactory)
        {
            // Store the injected session factory for use in controller methods
            _sessionFactory = sessionFactory;
        }
        // GET: api/Accounts
        [HttpGet]
        // List method handles GET requests to "api/Accounts".
        // It retrieves all accounts from the database and returns them as a collection of AccountDto objects.
        // The return type ActionResult<IEnumerable<AccountDto>> allows us to return:
        //    - HTTP 200 OK with the list of accounts
        //    - Other HTTP status codes if needed (e.g., error responses)
        // The list of accounts is returned as an IEnumerable, which represents a sequence of AccountDto objects.
        // The CancellationToken parameter allows the request to be cancelled (e.g., if the client disconnects)
        //  before the database query or processing is complete.
        public async Task<ActionResult<IEnumerable<AccountDto>>> List(CancellationToken cancellationToken)
        {
            // Automatically opening/closing the connection using  and
            // supporting cancellation
            //.configureAwait(false); will improve performance by avoiding context capture
            await using var dbContext = await _sessionFactory.CreateContextAsync(cancellationToken)
                .ConfigureAwait(false);
            // sql query to select all relevant fields from the account table
            const string sql = @" SELECT UID, LocationUid, Guid, CreatedUtc, UpdatedUtc, Status, EndDateUtc, 
                              AccountType, PaymentAmount, PendCancel, PendCancelDateUtc, 
                              PeriodStartUtc, PeriodEndUtc, NextBillingUtc
                              FROM account;";
            // Using SqlBuilder to construct the SQL query
            var builder = new SqlBuilder();
            // Adding the SQL query to the builder
            var template = builder.AddTemplate(sql);
            // Executing the query asynchronously and mapping the results to AccountDto objects
            // dbContext.Session is the database connection/session
            // QueryAsync is a Dapper extension method to execute the query and map results
            // template.RawSql contains the final SQL query
            // template.Parameters contains the values.
            // dbContext.Transaction is the current transaction (if any) --
            //  (Keeps data consistent if multiple changes happen at once in the database)
            var rows = await dbContext.Session.QueryAsync<AccountDto>(template.RawSql, template.Parameters, dbContext.Transaction)
                .ConfigureAwait(false);
            //Without Commit(), any changes made in the transaction are discarded (rolled back) when the transaction ends.
            dbContext.Commit();
            // Sends the data (rows) back to the client.
            return Ok(rows); // Returns an HTTP 200 OK status with the data
        }


        // POST: api/accounts
        // Create method handles POST requests to "api/accounts".
        // It creates a new account in the database using the data provided in the request body.
        [HttpPost]
        public async Task<ActionResult<string>> Create([FromBody] AccountDto model, CancellationToken cancellationToken)
        {
            // Automatically opening/closing the connection using  and
            // supporting cancellation
            //.configureAwait(false); will improve performance by avoiding context capture
            await using var dbContext = await _sessionFactory.CreateContextAsync(cancellationToken)
                .ConfigureAwait(false);
            // sql query to insert a new account record into the account table
            const string sql = @"
                INSERT INTO account ( LocationUid, Guid, CreatedUtc, UpdatedUtc, Status, EndDateUtc, 
                                        AccountType, PaymentAmount, PendCancel, PendCancelDateUtc, 
                                        PeriodStartUtc, PeriodEndUtc, NextBillingUtc)
                     VALUES (@LocationUid,@Guid, @CreatedUtc, @UpdatedUtc, @Status, @EndDateUtc, @AccountType, @PaymentAmount,
                    @PendCancel, @PendCancelDateUtc, @PeriodStartUtc, @PeriodEndUtc, @NextBillingUtc );";
            // Using SqlBuilder to construct the SQL query
            var builder = new SqlBuilder();
            // Adding the SQL query to the builder with parameters from the model   
            var template = builder.AddTemplate(sql, new AccountDto
            {
                Guid = Guid.NewGuid(),
                LocationUid = model.LocationUid,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow,
                Status = model.Status,
                EndDateUtc = model.EndDateUtc,
                AccountType = model.AccountType,
                PaymentAmount = model.PaymentAmount,
                PendCancel = model.PendCancel,
                PendCancelDateUtc = model.PendCancelDateUtc,
                PeriodStartUtc = model.PeriodStartUtc,
                PeriodEndUtc = model.PeriodEndUtc,
                NextBillingUtc = model.NextBillingUtc
            });
            // Executing the insert query asynchronously
            // dbContext.Session is the database connection/session
            // ExecuteAsync is a Dapper extension method to execute the query
            // template.RawSql contains the final SQL query
            // template.Parameters contains the values.
            // dbContext.Transaction is the current transaction (if any) --
            //  (Keeps data consistent if multiple changes happen at once in the database)
            var count = await dbContext.Session.ExecuteAsync(template.RawSql, template.Parameters, dbContext.Transaction)
                .ConfigureAwait(false);
            // Commit the transaction to save changes to the database
            dbContext.Commit();
            if (count == 1)
                return Ok();
            else
                return BadRequest("Unable to add location");
        }



        // GET: api/accounts/{Guid}
        [HttpGet("{id:Guid}")]
        public async Task<ActionResult<AccountDto>> Read(Guid id, CancellationToken cancellationToken)
        {
            await using var dbContext = await _sessionFactory.CreateContextAsync(cancellationToken)
                .ConfigureAwait(false);

            const string sql = @" SELECT
                    UID, LocationUid, Guid, CreatedUtc, UpdatedUtc, Status, EndDateUtc, 
                    AccountType, PaymentAmount, PendCancel, PendCancelDateUtc, 
                    PeriodStartUtc, PeriodEndUtc, NextBillingUtc
                FROM account
                /**where**/;";

            var builder = new SqlBuilder();

            var template = builder.AddTemplate(sql);

            builder.Where("Guid = @Guid", new
            {
                Guid = id
            });

            var rows = await dbContext.Session.QueryAsync<AccountDto>(template.RawSql, template.Parameters, dbContext.Transaction)
                .ConfigureAwait(false);

            dbContext.Commit();

            return Ok(rows.FirstOrDefault()); // Returns an HTTP 200 OK status with the data
        }


        // Update: api/accounts}
        [HttpPut]
        public async Task<ActionResult<AccountDto>> Update([FromBody] AccountDto model, CancellationToken cancellationToken)
        {
            await using var dbContext = await _sessionFactory.CreateContextAsync(cancellationToken)
                .ConfigureAwait(false);

            const string sql = @"UPDATE account
                                SET LocationUid = @LocationUid
                                WHERE Guid = @Guid;";

            var builder = new SqlBuilder();

            var template = builder.AddTemplate(sql, new AccountDto
            {
                Guid = model.Guid,
                LocationUid = model.LocationUid,
            });

            var count = await dbContext.Session.ExecuteAsync(template.RawSql, template.Parameters, dbContext.Transaction)
                .ConfigureAwait(false);

            dbContext.Commit();

            if (count == 1)
                return Ok();
            else
                return BadRequest("Unable to delete account");
        }


        // DELETE: api/locations/{Guid}
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<string>> Delete(int id, CancellationToken cancellationToken)
        {
            // Automatically opening/closing the connection using  and
            // supporting cancellation
            //.configureAwait(false); will improve performance by avoiding context capture
            await using var dbContext = await _sessionFactory.CreateContextAsync(cancellationToken)
                .ConfigureAwait(false);
            // sql query to delete a account record from the account table
            // First delete members associated with the account to maintain referential integrity
            const string sql = "DELETE FROM member WHERE AccountUid = @AccountUid";
            var builder = new SqlBuilder();
            // Adding the SQL query to the builder with parameters from the model
            var template = builder.AddTemplate(sql, new
            {
                AccountUid = id
            });
            // Executing the delete query asynchronously
            // dbContext.Session is the database connection/session
            // ExecuteAsync is a Dapper extension method to execute the query
            // template.RawSql contains the final SQL query
            // template.Parameters contains the values.
            // dbContext.Transaction is the current transaction (if any) --
            //  (Keeps data consistent if multiple changes happen at once in the database)
            var count = await dbContext.Session.ExecuteAsync(template.RawSql, template.Parameters, dbContext.Transaction)
                .ConfigureAwait(false);

            // Now delete the account itself
            // sql query to delete a account record from the account table
            // After deleting associated members
            const string sqlOne = "DELETE FROM account WHERE UID = @UID";
            var builderOne = new SqlBuilder();
            // Adding the SQL query to the builder with parameters from the model
            var templateOne = builderOne.AddTemplate(sqlOne, new
            {
                UID = id,
            });
            // Executing the delete query asynchronously
            // dbContext.Session is the database connection/session
            // ExecuteAsync is a Dapper extension method to execute the query
            // template.RawSql contains the final SQL query
            // template.Parameters contains the values.
            // dbContext.Transaction is the current transaction (if any) --
            //  (Keeps data consistent if multiple changes happen at once in the database)
            var countOne = await dbContext.Session.ExecuteAsync(templateOne.RawSql, templateOne.Parameters, dbContext.Transaction)
                .ConfigureAwait(false);

            dbContext.Commit();

            if (countOne == 1)
            {
                return Ok();
            }
            else
            {
                return BadRequest("Unable to delete account ");
            }
        }

   
   
     // Q3: Add an endpoint to the accounts controller that will return all members
     //  for a specified account (using the account's Guid)
     // GET: api/accounts/{Guid}
        [HttpGet("{id:Guid}/members")]
        public async Task<ActionResult<IEnumerable<MemberDto>>> ListByGuid(Guid id, CancellationToken cancellationToken)
        {
            // Automatically opening/closing the connection using  and
            // supporting cancellation
            //.configureAwait(false); will improve performance by avoiding context capture
            await using var dbContext = await _sessionFactory.CreateContextAsync(cancellationToken)
                .ConfigureAwait(false);
            // sql query to select all relevant fields from the Members table for a specific account Guid
            // Joining account and member tables to get members for the specified account
            // Using INNER JOIN to link accounts to their members based on AccountUid
            // Filtering by the provided account Guid
            // Selecting only the required fields for the PersonDto
            const string sql = @"
                                SELECT 
                                    m.FirstName,
                                    m.LastName,
                                    m.Address,
                                    m.City,
                                    m.Locale,
                                    m.PostalCode
                                FROM account a
                                INNER JOIN member m ON a.UID = m.AccountUid
                                WHERE a.Guid = @Guid";
            // Using SqlBuilder to construct the SQL query
            var builder = new SqlBuilder();
            // Adding the SQL query to the builder with the account Guid parameter
            var template = builder.AddTemplate(sql, new { Guid = id });
            // Executing the query asynchronously and mapping the results to PersonDto objects
            // dbContext.Session is the database connection/session
            // QueryAsync is a Dapper extension method to execute the query and map results
            // template.RawSql contains the final SQL query
            // template.Parameters contains the values.
            // dbContext.Transaction is the current transaction (if any) --
            //  (Keeps data consistent if multiple changes happen at once in the database)
            var rows = await dbContext.Session.QueryAsync<PersonDto>(template.RawSql, template.Parameters, dbContext.Transaction)
                .ConfigureAwait(false);
            //Without Commit(), any changes made in the transaction are discarded (rolled back) when the transaction ends.
            dbContext.Commit();
            return Ok(rows); // Returns an HTTP 200 OK status with the data
        }
   
    }
    
    


    

}

