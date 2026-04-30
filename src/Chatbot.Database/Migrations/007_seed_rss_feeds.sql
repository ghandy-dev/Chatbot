INSERT INTO 
	[rss_feed_categories]
	([category_id], [category])
VALUES 
	(1, "Top Stories"),
	(2, "World"),
	(3, "US"),
	(4, "UK"),
	(5, "Business"),
	(6, "Politics"),
	(7, "Health"),
	(8, "Education"),
	(9, "Science"),
	(10, "Technology"),
	(11, "Entertainment"),
	(12, "Sports")
ON CONFLICT([category_id]) DO NOTHING;

INSERT INTO
	[rss_feeds]
	([category_id], [url])
VALUES
	(1, "https://feeds.bbci.co.uk/news/rss.xml"),
	(2, "https://feeds.bbci.co.uk/news/world/rss.xml"),
	(4, "https://feeds.bbci.co.uk/news/uk/rss.xml"),
	(5, "https://feeds.bbci.co.uk/news/business/rss.xml"),
	(6, "https://feeds.bbci.co.uk/news/politics/rss.xml"),
	(7, "https://feeds.bbci.co.uk/news/health/rss.xml"),
	(8, "http://feeds.bbci.co.uk/news/education/rss.xml"),
	(9, "http://feeds.bbci.co.uk/news/science_and_environment/rss.xml"),
	(10, "http://feeds.bbci.co.uk/news/technology/rss.xml"),
	(11, "http://feeds.bbci.co.uk/news/entertainment_and_arts/rss.xml"),

	(1, "http://feeds.skynews.com/feeds/rss/home.xml"),
	(4, "http://feeds.skynews.com/feeds/rss/uk.xml"),
	(2, "http://feeds.skynews.com/feeds/rss/world.xml"),
	(3, "http://feeds.skynews.com/feeds/rss/us.xml"),
	(5, "http://feeds.skynews.com/feeds/rss/business.xml"),
	(6, "http://feeds.skynews.com/feeds/rss/politics.xml"),
	(10, "http://feeds.skynews.com/feeds/rss/technology.xml"),
	(11, "http://feeds.skynews.com/feeds/rss/entertainment.xml"),
	-- (?, "http://feeds.skynews.com/feeds/rss/strange.xml")
	
	(1, "https://rss.nytimes.com/services/xml/rss/nyt/HomePage.xml"),
	(2, "https://rss.nytimes.com/services/xml/rss/nyt/World.xml"),
	(2, "https://rss.nytimes.com/services/xml/rss/nyt/Africa.xml"),
	(2, "https://rss.nytimes.com/services/xml/rss/nyt/Americas.xml"),
	(2, "https://rss.nytimes.com/services/xml/rss/nyt/AsiaPacific.xml"),
	(2, "https://rss.nytimes.com/services/xml/rss/nyt/Europe.xml"),
	(2, "https://rss.nytimes.com/services/xml/rss/nyt/MiddleEast.xml"),
	(3, "https://rss.nytimes.com/services/xml/rss/nyt/US.xml"),
	(3, "https://rss.nytimes.com/services/xml/rss/nyt/Education.xml"),
	(3, "https://rss.nytimes.com/services/xml/rss/nyt/Politics.xml"),
	(5, "https://rss.nytimes.com/services/xml/rss/nyt/Business.xml"),
	(10, "https://rss.nytimes.com/services/xml/rss/nyt/Technology.xml"),
	(12, "https://rss.nytimes.com/services/xml/rss/nyt/Sports.xml"),
	(9, "https://rss.nytimes.com/services/xml/rss/nyt/Science.xml"),
	(9, "https://rss.nytimes.com/services/xml/rss/nyt/Climate.xml"),
	(7, "https://rss.nytimes.com/services/xml/rss/nyt/Health.xml"),
	(11, "https://rss.nytimes.com/services/xml/rss/nyt/Arts.xml"),
	(11, "https://rss.nytimes.com/services/xml/rss/nyt/Movies.xml"),
	(11, "https://rss.nytimes.com/services/xml/rss/nyt/Books/Review.xml"),
	(11, "https://rss.nytimes.com/services/xml/rss/nyt/Dance.xml"),
	(11, "https://rss.nytimes.com/services/xml/rss/nyt/Music.xml"),
	(11, "https://rss.nytimes.com/services/xml/rss/nyt/Television.xml"),
	(11, "https://rss.nytimes.com/services/xml/rss/nyt/Theater.xml"),

	(2, "https://feeds.a.dj.com/rss/RSSWorldNews.xml"),
	(5, "https://feeds.a.dj.com/rss/WSJcomUSBusiness.xml"),
	(5, "https://feeds.a.dj.com/rss/RSSMarketsMain.xml"),
	(10, "https://feeds.a.dj.com/rss/RSSWSJD.xml"),
	(7, "https://feeds.a.dj.com/rss/RSSLifestyle.xml"),

	(1, "https://news.yahoo.com/rss/topstories"),
	(2, "https://news.yahoo.com/rss"),
	(3, "https://news.yahoo.com/rss/us/"),
	(5, "https://finance.yahoo.com/rss/topstories"),
	(5, "https://news.yahoo.com/rss/business"),
	(6, "https://news.yahoo.com/rss/politics"),
	(7, "https://news.yahoo.com/rss/health"),
	(9, "https://news.yahoo.com/rss/science"),
	(10, "https://news.yahoo.com/rss/tech/"),
	(11, "https://news.yahoo.com/rss/entertainment"),
	(12, "https://news.yahoo.com/rss/sports")
ON CONFLICT ([url]) DO NOTHING;