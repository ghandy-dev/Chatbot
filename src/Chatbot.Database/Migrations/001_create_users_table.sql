CREATE TABLE IF NOT EXISTS [users] (
    [user_id] INT PRIMARY KEY NOT NULL,
    [username] TEXT NOT NULL,
    [is_admin] BOOLEAN DEFAULT 0 NOT NULL
) WITHOUT ROWID;

CREATE INDEX IF NOT EXISTS idx_users_user_id ON users ([user_id]);