using SqlSugar;

namespace Weblog.Core.Model.Entities;

[SugarTable("t_article")]
public class Article
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 文章标题
    /// </summary>
    [SugarColumn(Length = 120)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 文章封面
    /// </summary>
    [SugarColumn(Length = 120)]
    public string Cover { get; set; } = string.Empty;

    /// <summary>
    /// 文章摘要
    /// </summary>
    [SugarColumn(Length = 160, IsNullable = true)]
    public string? Summary { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 最后一次更新时间
    /// </summary>
    public DateTime UpdateTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 删除标志位：0：未删除 1：已删除
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 被阅读次数
    /// </summary>
    public int ReadNum { get; set; } = 1;

    /// <summary>
    /// 文章权重，用于是否置顶（0: 未置顶；>0: 参与置顶，权重值越高越靠前）
    /// </summary>
    public int Weight { get; set; } = 0;

    /// <summary>
    /// 文章类型 - 1：普通文章，2：收录于知识库
    /// </summary>
    public int Type { get; set; } = 1;

    /// <summary>
    /// 状态（0：未发布；1：已发布）
    /// </summary>
    public int Status { get; set; } = 1;


    /// <summary>
    /// 分类ID（用于DTO映射，不存储在数据库中）
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public long CategoryId { get; set; }

    /// <summary>
    /// 分类名称（用于DTO映射，不存储在数据库中）
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public string? CategoryName { get; set; }
}
