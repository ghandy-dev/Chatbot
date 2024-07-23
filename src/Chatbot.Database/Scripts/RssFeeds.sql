CREATE TABLE IF NOT EXISTS [rss_feed_categories] (
    [category_id] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    [category] TEXT NOT NULL,

    UNIQUE([category])
);

CREATE TABLE IF NOT EXISTS [rss_feeds] (
    [rss_feed_id] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    [category_id] INT NOT NULL,
    [url] TEXT NOT NULL,

    UNIQUE([url]),
    FOREIGN KEY ([category_id]) REFERENCES [rss_feed_categories] ([category_id])
);

CREATE INDEX IF NOT EXISTS idx_rss_feeds_category_id ON [rss_feeds] ([category_id]);