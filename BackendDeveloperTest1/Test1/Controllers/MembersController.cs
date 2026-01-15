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
    // So the MembersController will respond to respond to (api/accounts)"
    [Route("api/[controller]")]
    public class MembersController : ControllerBase //base class for API controllers only not views
    {
        // Session factory for database connections
        // It is used for managing connection and transactions to the database
        // no need to create connectionstring manually
        private readonly ISessionFactory _sessionFactory;
        // The session factory used to get database sessions. 
        // Each session is connected to the database file (test1.db),
        public MembersController(ISessionFactory sessionFactory)
        {
            // Store the injected session factory for use in controller methods
            _sessionFactory = sessionFactory;
        }
        // GET: api/Members
        [HttpGet]
        // List method handles GET requests to "api/Memberss".
        // It retrieves all Memberss from the database and returns them as a collection of MembersDto objects.
        // The return type ActionResult<IEnumerable<MembersDto>> allows us to return:
        //    - HTTP 200 OK with the list of Memberss
        //    - Other HTTP status codes if needed (e.g., error responses)
        // The list of Memberss is returned as an IEnumerable, which represents a sequence of MembersDto objects.
        // The CancellationToken parameter allows the request to be cancelled (e.g., if the client disconnects)
        //  before the database query or processing is complete.
        public async Task<ActionResult<IEnumerable<MemberDto>>> List(CancellationToken cancellationToken)
        {
            // Automatically opening/closing the connection using  and
            // supporting cancellation
            //.configureAwait(false); will improve performance by avoiding context capture
            // "Primary" is a reserved keyword in SQL, so it is enclosed in double quotes to avoid syntax errors.
            await using var dbContext = await _sessionFactory.CreateContextAsync(cancellationToken)
                .ConfigureAwait(false);
            // sql query to select all relevant fields from the Members table
            const string sql = @" SELECT UID, Guid, Guid, AccountUid, LocationUid, CreatedUtc, UpdatedUtc,
                                    ""Primary"", JoinedDateUtc,CancelDateUtc,FirstName,LastName,
                                    Address,City,Locale,PostalCode,Cancelled
                                    FROM member";
            // Using SqlBuilder to construct the SQL query
            var builder = new SqlBuilder();
            // Adding the SQL query to the builder
            var template = builder.AddTemplate(sql);
            // Executing the query asynchronously and mapping the results to MembersDto objects
            // dbContext.Session is the database connection/session
            // QueryAsync is a Dapper extension method to execute the query and map results
            // template.RawSql contains the final SQL query
            // template.Parameters contains the values.
            // dbContext.Transaction is the current transaction (if any) --
            //  (Keeps data consistent if multiple changes happen at once in the database)
            var rows = await dbContext.Session.QueryAsync<MemberDto>(template.RawSql, template.Parameters, dbContext.Transaction)
                .ConfigureAwait(false);
            //Without Commit(), any changes made in the transaction are discarded (rolled back) when the transaction ends.
            dbContext.Commit();
            // Sends the data (rows) back to the client.
            return Ok(rows); // Returns an HTTP 200 OK status with the data
        }


        // POST: api/Memberss
        // Create method handles POST requests to "api/Memberss".
        // It creates a new Members in the database using the data provided in the request body.
        // Q4A The create endpoint should only allow for one primary member per account.
        [HttpPost]
        public async Task<ActionResult<string>> Create([FromBody] MemberDto model, CancellationToken cancellationToken)
        {
            // Automatically opening/closing the connection using  and
            // supporting cancellation
            //.configureAwait(false); will improve performance by avoiding context capture
            await using var dbContext = await _sessionFactory.CreateContextAsync(cancellationToken)
                .ConfigureAwait(false);
            // Check if a primary member already exists for this account
            // COUNT is used to count the number of rows that match the criteria
            // If the count is greater than 0, it means a primary member already exists)
            // Primary is a reserved keyword in SQL, so it is enclosed in double quotes to avoid syntax errors.
            const string checkPrimarySql = @"
                                                SELECT COUNT(*) 
                                                FROM member 
                                                WHERE AccountUid = @AccountUid AND ""Primary"" = 1;";
            // Using SqlBuilder to construct the SQL query
            var builder = new SqlBuilder();
            var template = builder.AddTemplate(checkPrimarySql, new MemberDto
            {
                AccountUid = model.AccountUid
            });
            // Execute the query to get the count of primary members for the given account
            // If the count is greater than 0, it means a primary member already exists
            var primaryCount = await dbContext.Session.ExecuteScalarAsync<int>(template.RawSql, template.Parameters, dbContext.Transaction)
             .ConfigureAwait(false);
            if (primaryCount > 0)
            {
                return BadRequest("A primary member already exists for this account.");
            }
            // If there is no primary member, proceed to insert the new member
            else
            {
                // Insert new member
                const string insertSql = @"
                INSERT INTO member 
                (Guid, AccountUid, LocationUid, CreatedUtc, UpdatedUtc, ""Primary"", JoinedDateUtc, CancelDateUtc,
                FirstName, LastName, Address, City, Locale, PostalCode, Cancelled)
                VALUES
                (@Guid, @AccountUid, @LocationUid, @CreatedUtc, @UpdatedUtc, @Primary, @JoinedDateUtc, @CancelDateUtc,
                @FirstName, @LastName, @Address, @City, @Locale, @PostalCode, @Cancelled);";

                // Using SqlBuilder to construct the SQL query
                var builderOne = new SqlBuilder();
                // Adding the SQL query to the builder
                var templateOne = builderOne.AddTemplate(insertSql, new MemberDto
                {
                    Guid = model.Guid,
                    AccountUid = model.AccountUid,
                    LocationUid = model.LocationUid,
                    CreatedUtc = model.CreatedUtc,
                    UpdatedUtc = model.UpdatedUtc,
                    Primary = model.Primary,
                    JoinedDateUtc = model.JoinedDateUtc,
                    CancelDateUtc = model.CancelDateUtc,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Address = model.Address,
                    City = model.City,
                    Locale = model.Locale,
                    PostalCode = model.PostalCode,
                    Cancelled = model.Cancelled
                });
                // Executing the insert query asynchronously
                await dbContext.Session.ExecuteAsync(templateOne.RawSql, templateOne.Parameters, dbContext.Transaction)
                 .ConfigureAwait(false);

                dbContext.Commit();
                return Ok();
            }
        }


        // DELETE: api/Memberss/{accountUid}/members
        // Q4C Implement an endpoint to delete all members of an account except the primary member
        [HttpDelete("{accountUid:int}/members")]
        public async Task<IActionResult> DeleteAllExceptPrimary(int accountUid, CancellationToken cancellationToken)
        {
            // Automatically opening/closing the connection using  and
            // supporting cancellation
            //.configureAwait(false); will improve performance by avoiding context capture
            await using var dbContext = await _sessionFactory.CreateContextAsync(cancellationToken)
                .ConfigureAwait(false);

            // Count members 
            // COUNT is used to count the number of rows that match the criteria
            // If the count is less than or equal to 1, it means there is only one member (the primary one)
            const string countSql = @"
                SELECT COUNT(1)
                FROM member
                WHERE AccountUid = @AccountUid";
            // Execute the query to get the count of members for the given account
            // ExecuteScalarAsync is used to execute the query and return a single value (the count)
            // dbContext.Session is the database connection/session
            // countSql is the SQL query to execute
            // new { AccountUid = accountUid } provides the parameter value for the query
            // dbContext.Transaction is the current transaction (if any)
            int count = await dbContext.Session.ExecuteScalarAsync<int>(
                countSql, new { AccountUid = accountUid }, dbContext.Transaction);

            if (count <= 1)
                return BadRequest("Cannot delete members when only one exists.");

            //  Delete all except primary
            // "Primary" is a reserved keyword in SQL, so it is enclosed in double quotes to avoid syntax errors.
            // The SQL query deletes all members for the specified AccountUid where the Primary flag is 0 (non-primary members).
            const string deleteSql = @"
                DELETE FROM member
                WHERE AccountUid = @AccountUid
                AND Primary = 0";
            // Using SqlBuilder to construct the SQL query
            var builderOne = new SqlBuilder();
            // Adding the SQL query to the builder
            var template = builderOne.AddTemplate(deleteSql, new
            {
                AccountUid = accountUid
            });
            await dbContext.Session.ExecuteAsync(template.RawSql, template.Parameters, dbContext.Transaction);
            dbContext.Commit();
            return Ok("All non-primary members deleted.");
        }


        // DELETE: api/Memberss/{id}
        // Q4B The delete endpoint should make the next member on the account the primary member, when the primary member
        // is deleted for an account. The endpoint should not allow deletion of the last member on the account..
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteMember(int id, CancellationToken cancellationToken)
        {
            await using var dbContext = await _sessionFactory.CreateContextAsync(cancellationToken)
                .ConfigureAwait(false);
            //  Get member info
            // Primary is a reserved keyword in SQL, so it is enclosed in double quotes to avoid syntax errors.
            // The SQL query retrieves the UID, AccountUid, and Primary status of the member with the specified UID.
            var memberSql = @" SELECT UID, AccountUid, ""Primary""
                FROM member
                WHERE UID = @Id";

            var builder = new SqlBuilder();
            var template = builder.AddTemplate(memberSql, new { Id = id });
            // Using FirstOrDefault to get a single member or null if not found
            var rows = (await dbContext.Session.QueryAsync<MemberDto>(template.RawSql, template.Parameters, dbContext.Transaction)
                .ConfigureAwait(false)).FirstOrDefault();
            // If member not found, return 404 Not Found
            if (rows == null)
                return NotFound();

            // Count members in the account
            // COUNT is used to count the number of rows that match the criteria
            // If the count is 1, it means this is the last member of the account
            var memberCountSql = @"
                SELECT COUNT(1)
                FROM member
                WHERE AccountUid = @AccountUid";
            // Execute the query to get the count of members for the given account
            // ExecuteScalarAsync is used to execute the query and return a single value (the count)
            // dbContext.Session is the database connection/session
            // memberCountSql is the SQL query to execute
            // new MemberDto { AccountUid = rows.AccountUid } provides the parameter value for the query
            // dbContext.Transaction is the current transaction (if any)
            var builderCount = new SqlBuilder();
            var templateCount = builderCount.AddTemplate(memberCountSql, new MemberDto { AccountUid = rows.AccountUid });

            int count = await dbContext.Session.ExecuteScalarAsync<int>(templateCount.RawSql, templateCount.Parameters, dbContext.Transaction)
                        .ConfigureAwait(false);
            // If only one member exists, cannot delete
            if (count == 1)
                return BadRequest("Cannot delete the last member of the account.");
            // Delete the member
            // The SQL query deletes the member with the specified UID.
            var deleteSql = @"
                        DELETE FROM member 
                        WHERE UID = @Id";
            var builderDelete = new SqlBuilder();
            var templateDelete = builderDelete.AddTemplate(deleteSql, new { Id = id });
            // Executing the delete query asynchronously
            await dbContext.Session.ExecuteAsync(templateDelete.RawSql, templateDelete.Parameters, dbContext.Transaction)
                    .ConfigureAwait(false);
            // Promote new primary if needed
            if (rows.Primary == 1)
            {
                // The SQL query updates the member with the earliest JoinedDateUtc to be the new primary member
                // The subquery selects the UID of the member with the earliest JoinedDateUtc for the given AccountUid.
                // JoinedDateUtc indicates when the member joined the account.
                // This ensures that when the primary member is deleted, the next member who joined the account becomes the new primary member.
                // "Primary" is a reserved keyword in SQL, so it is enclosed in double quotes to avoid syntax errors.
                // Limit 1 ensures that only one member is promoted to primary.
                var promoteSql = @"
                    UPDATE member
                    SET ""Primary"" = 1
                    WHERE UID = (
                        SELECT UID
                        FROM member
                        WHERE AccountUid = @AccountUid
                        ORDER BY JoinedDateUtc
                        LIMIT 1
                    )";
                // Using SqlBuilder to construct the SQL query
                var builderPromote = new SqlBuilder();
                var templatePromote = builderPromote.AddTemplate(promoteSql, new { AccountUid = rows.AccountUid });
                // Executing the promote query asynchronously
                // Promotes the next member who joined the account to be the new primary member
                await dbContext.Session.ExecuteAsync(templatePromote.RawSql, templatePromote.Parameters, dbContext.Transaction)
                .ConfigureAwait(false);
            }
            // Commit the transaction to save changes
            // Without Commit(), any changes made in the transaction are discarded (rolled back) when the transaction ends.
            dbContext.Commit();
            return Ok("Member deleted successfully.");
        }




        // Q6. Document any recommendations on table indexes.
                // "member" table column "Primary" is a reserved SQL keyword.
                // It is advisable to rename this column to avoid potential conflicts and improve code readability.
        // Q7. Document any recommendations on improving the structure or maintainability of the code.
                // Add model validation (e.g., [Required] attributes) 
                // to ensure incoming data is correct before processing.   
                // Move LocationDto to its own file in a Dtos or Models folder. 
                // This keeps controllers focused and makes DTOs reusable.
                // Needs to add Detailed Comments throughout the code to explain 
                // the purpose and functionality of each section.
    }


}


