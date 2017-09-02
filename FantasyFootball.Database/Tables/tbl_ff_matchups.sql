CREATE TABLE [dbo].[tbl_ff_matchups]
(
	[MatchupId] INT NOT NULL PRIMARY KEY IDENTITY, 
    [HomeTeam] VARCHAR(50) NOT NULL, 
    [AwayTeam] VARCHAR(50) NOT NULL, 
    [Season] INT NOT NULL, 
    [Week] INT NOT NULL, 
    [Date] DATETIME NOT NULL
)
