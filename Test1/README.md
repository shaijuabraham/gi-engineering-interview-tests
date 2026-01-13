# Instructions
## Overview
This project provides the skeleton of a Gym management REST web application using SQLite and Dapper. The database is prepopulated with the following entities:
* __location__ - A table containing gym locations.
* __account__ - A table containing gym membership accounts. There is a many-to-one relationship between accounts and locations.
* __member__ - A table containing the members of accounts.  There is a many-to-one relationship between members and accounts.

Verify your endpoints work by using Postman (https://www.postman.com/downloads/).  The app will listen on port 8080.

## Tasks to complete

1. Create an accounts controller with list, create, read, update and delete endpoints.
2. Enhance the location's list endpoint to return the number of non-cancelled accounts (where the `account.Status` < [CANCELLED](Test1/Models/AccountStatusType.cs)) for each location.
3. Add an endpoint to the accounts controller that will return all members for a specified account (using the account's Guid)
4. Create a members controller that will list, create, and delete members. 
   * The create endpoint should only allow for one primary member per account.
   * The delete endpoint should make the next member on the account the primary member, when the primary member is deleted for an account.  The endpoint should not allow deletion of the last member on the account.
5. Add an endpoint to the accounts controller that will delete all members of a specified account except the "primary" member.
6. Document any recommendations on table indexes.
7. Document any recommendations on improving the structure or maintainability of the code.

