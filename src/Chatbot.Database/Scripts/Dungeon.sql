CREATE TABLE IF NOT EXISTS [dungeon] (
    [user_id] INT PRIMARY KEY NOT NULL,
    [data] TEXT NOT NULL
) WITHOUT ROWID;

CREATE INDEX IF NOT EXISTS idx_dungeon_user_id ON [dungeon] ([user_id]);