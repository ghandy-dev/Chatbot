CREATE TABLE IF NOT EXISTS [reminders] (
    [reminder_id] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    [timestamp] TEXT NOT NULL,
    [from_user_id] INT NOT NULL,
    [from_username] TEXT NOT NULL,
    [target_user_id] INT NOT NULL,
    [target_username] TEXT NOT NULL,
    [channel] TEXT NULL,
    [message] TEXT NOT NULL,
    [reminder_timestamp] TEXT NULL,
    [reminded] INT NOT NULL DEFAULT FALSE
);

CREATE INDEX idx_target_user_id ON reminders ([target_user_id]);
CREATE INDEX idx_reminded ON reminders ([reminded]);