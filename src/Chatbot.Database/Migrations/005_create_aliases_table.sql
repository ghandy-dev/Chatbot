CREATE TABLE IF NOT EXISTS [aliases] (
    [alias_id] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    [user_id] INT NOT NULL,
    [name] TEXT NOT NULL,
    [command] TEXT NOT NULL,

    FOREIGN KEY ([user_id]) REFERENCES [users] ([user_id]),
    UNIQUE([user_id], [name])
);

CREATE INDEX IF NOT EXISTS idx_aliases_user_id ON aliases ([user_id]);