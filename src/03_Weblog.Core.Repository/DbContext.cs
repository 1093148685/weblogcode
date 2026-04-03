using SqlSugar;
using Weblog.Core.Model.Entities;

namespace Weblog.Core.Repository;

public class DbContext
{
    private readonly ISqlSugarClient _db;

    public DbContext(ISqlSugarClient db)
    {
        _db = db;
    }

    public ISqlSugarClient Db => _db;

    public ISugarQueryable<SysUser> SysUserDb => _db.Queryable<SysUser>();
    public ISugarQueryable<Category> CategoryDb => _db.Queryable<Category>();
    public ISugarQueryable<Tag> TagDb => _db.Queryable<Tag>();
    public ISugarQueryable<Article> ArticleDb => _db.Queryable<Article>();
    public ISugarQueryable<ArticleTag> ArticleTagDb => _db.Queryable<ArticleTag>();
    public ISugarQueryable<BlogSettings> BlogSettingsDb => _db.Queryable<BlogSettings>();
    public ISugarQueryable<Statistics> StatisticsDb => _db.Queryable<Statistics>();
    public ISugarQueryable<ArticleContent> ArticleContentDb => _db.Queryable<ArticleContent>();
    public ISugarQueryable<Comment> CommentDb => _db.Queryable<Comment>();
    public ISugarQueryable<Wiki> WikiDb => _db.Queryable<Wiki>();
    public ISugarQueryable<WikiCatalog> WikiCatalogDb => _db.Queryable<WikiCatalog>();
    public ISugarQueryable<StatisticsArticlePv> StatisticsArticlePvDb => _db.Queryable<StatisticsArticlePv>();
    public ISugarQueryable<ArticleCategoryRel> ArticleCategoryRelDb => _db.Queryable<ArticleCategoryRel>();
    public ISugarQueryable<UserRole> UserRoleDb => _db.Queryable<UserRole>();
    public ISugarQueryable<FlowerRecord> FlowerRecordDb => _db.Queryable<FlowerRecord>();
    public ISugarQueryable<StickerPack> StickerPackDb => _db.Queryable<StickerPack>();
    public ISugarQueryable<Sticker> StickerDb => _db.Queryable<Sticker>();
    public ISugarQueryable<CommentAdmin> CommentAdminDb => _db.Queryable<CommentAdmin>();
    public ISugarQueryable<LinkPreviewCache> LinkPreviewCacheDb => _db.Queryable<LinkPreviewCache>();
    public ISugarQueryable<AiProvider> AiProviderDb => _db.Queryable<AiProvider>();
    public ISugarQueryable<AiPlugin> AiPluginDb => _db.Queryable<AiPlugin>();
    public ISugarQueryable<AiConversation> AiConversationDb => _db.Queryable<AiConversation>();
    public ISugarQueryable<AiAgentLog> AiAgentLogDb => _db.Queryable<AiAgentLog>();
    public ISugarQueryable<AiAgentConfig> AiAgentConfigDb => _db.Queryable<AiAgentConfig>();

    // RAG 知识库
    public ISugarQueryable<KnowledgeBase> KnowledgeBaseDb => _db.Queryable<KnowledgeBase>();
    public ISugarQueryable<KbDocument> KbDocumentDb => _db.Queryable<KbDocument>();
    public ISugarQueryable<KbChunk> KbChunkDb => _db.Queryable<KbChunk>();
}
