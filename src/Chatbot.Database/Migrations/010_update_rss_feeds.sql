INSERT INTO
	[rss_feeds]
	([category_id], [url])
VALUES
    (2, "https://old.reddit.com/r/worldnews.rss"),
    (3, "https://old.reddit.com/r/news.rss"),
    (10, "https://old.reddit.com/r/technology.rss"),
    (11, "https://old.reddit.com/r/entertainment.rss")
ON CONFLICT([url]) DO NOTHING;