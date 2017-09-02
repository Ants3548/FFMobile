CREATE TABLE [dbo].[tbl_ff_players]
(
	[PlayerId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [FirstName] VARCHAR(50) NOT NULL, 
    [LastName] VARCHAR(50) NOT NULL,
	[Position] VARCHAR(10) NULL,
	[Team] VARCHAR(10) NULL,
	[JerseyNumber] VARCHAR(10) NULL,
	[Espn] VARCHAR(10) NULL,
	[Cbs] VARCHAR(10) NULL,
	[Nfl] VARCHAR(10) NULL,
	[Roto] VARCHAR(10) NULL,
	[Yahoo] VARCHAR(10) NULL
)
