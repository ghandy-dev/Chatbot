-- Rock Paper Scissors Stats
CREATE TABLE IF NOT EXISTS [rps_stats] (
    [rps_stats_id] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, 
    [user_id] INT NOT NULL,
    [score] INT NOT NULL,
    [total_moves] INT NOT NULL,
    [wins] INT NOT NULL,
    [losses] INT NOT NULL,
	
	FORIEGN KEY [user_id] REFERENCES [users] ([user_id]),
    UNIQUE([user_id])
);