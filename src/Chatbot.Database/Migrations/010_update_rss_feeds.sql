INSERT INTO
	[rss_feeds]
	([category_id], [url])
VALUES
	(1, "https://news.google.com/rss")
	(2, "https://news.yahoo.com/rss"),
ON CONFLICT([url]) DO NOTHING;