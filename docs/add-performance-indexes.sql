-- Run this against the netweblog database to add performance indexes.
-- These indexes eliminate full table scans on JOIN/WHERE lookups.

-- Article table: composite index on Status + IsDeleted (most common filter)
CREATE INDEX IF NOT EXISTS idx_status_deleted ON t_article (Status, IsDeleted);

-- Article table: index on CreateTime DESC (for ORDER BY CreateTime DESC)
CREATE INDEX IF NOT EXISTS idx_create_time ON t_article (CreateTime DESC);

-- Article-Category join table: index for lookups by ArticleId and CategoryId
CREATE INDEX IF NOT EXISTS idx_article_id ON t_article_category_rel (ArticleId);
CREATE INDEX IF NOT EXISTS idx_category_id ON t_article_category_rel (CategoryId);

-- Article-Tag join table: index for lookups by ArticleId and TagId
CREATE INDEX IF NOT EXISTS idx_article_id ON t_article_tag (ArticleId);
CREATE INDEX IF NOT EXISTS idx_tag_id ON t_article_tag (TagId);

-- Wiki-Catalog join table: index for lookups by WikiId and ArticleId
CREATE INDEX IF NOT EXISTS idx_wiki_id ON t_wiki_catalog (WikiId);
CREATE INDEX IF NOT EXISTS idx_article_id ON t_wiki_catalog (ArticleId);
