-- Runs once the database exists, as sa, before the owning service starts.
-- Creates the migration login and the request login. Idempotent.
IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'$(OwnerLogin)')
    CREATE LOGIN [$(OwnerLogin)] WITH PASSWORD = N'$(OwnerPassword)', CHECK_POLICY = ON;
GO
IF NOT EXISTS (SELECT 1 FROM sys.sql_logins WHERE name = N'$(RuntimeLogin)')
    CREATE LOGIN [$(RuntimeLogin)] WITH PASSWORD = N'$(RuntimePassword)', CHECK_POLICY = ON;
GO
USE [$(DatabaseName)];
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$(OwnerLogin)')
    CREATE USER [$(OwnerLogin)] FOR LOGIN [$(OwnerLogin)];
ALTER ROLE db_owner ADD MEMBER [$(OwnerLogin)];
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$(RuntimeLogin)')
    CREATE USER [$(RuntimeLogin)] FOR LOGIN [$(RuntimeLogin)];
ALTER ROLE db_datareader ADD MEMBER [$(RuntimeLogin)];
ALTER ROLE db_datawriter ADD MEMBER [$(RuntimeLogin)];
GO
