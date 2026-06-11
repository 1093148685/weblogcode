using System.Text.RegularExpressions;

namespace Weblog.Core.Service.AI.Routing;

public interface IAiChatRoutingService
{
    AiChatRoute Decide(AiChatRouteRequest request);
}

public class AiChatRoute
{
    public string Mode { get; set; } = "normal";
    public string Reason { get; set; } = "普通问题，直接回答更稳定";
    public string Intent { get; set; } = "general";
    public double Confidence { get; set; } = 0.75;
    public long? KbId { get; set; }
    public bool UseWebSearch => Mode == "web";
    public bool UseRag => Mode == "rag" && KbId.GetValueOrDefault() > 0;
    public bool UseArticle => Mode == "article";
}

public class AiChatRouteRequest
{
    public string RequestedMode { get; set; } = "normal";
    public string Query { get; set; } = string.Empty;
    public long? KbId { get; set; }
    public bool HasArticleContext { get; set; }
    public bool AllowAutoWebSearch { get; set; } = true;
    public bool PreferKnowledgeBase { get; set; } = true;
}

public class AiChatRoutingService : IAiChatRoutingService
{
    public AiChatRoute Decide(AiChatRouteRequest request)
    {
        var requested = NormalizeMode(request.RequestedMode);
        var query = request.Query?.Trim() ?? string.Empty;

        if (requested is "normal" or "web" or "rag" or "article")
        {
            return BuildManualRoute(requested, request);
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return new AiChatRoute
            {
                Mode = "normal",
                Intent = "empty",
                Reason = "没有识别到明确问题，保持普通聊天模式",
                Confidence = 0.9
            };
        }

        if (request.HasArticleContext && LooksLikeArticleQuestion(query))
        {
            return new AiChatRoute
            {
                Mode = "article",
                Intent = "article",
                Reason = "问题围绕当前文章内容，优先进入文章阅读辅助模式",
                Confidence = 0.92
            };
        }

        if (request.PreferKnowledgeBase
            && request.KbId.GetValueOrDefault() > 0
            && IsKnowledgeQuestion(query))
        {
            return new AiChatRoute
            {
                Mode = "rag",
                Intent = "knowledge",
                KbId = request.KbId,
                Reason = "问题提到博客、文章、知识库或站内资料，优先检索知识库",
                Confidence = 0.88
            };
        }

        if (request.AllowAutoWebSearch && NeedsFreshWebData(query))
        {
            return new AiChatRoute
            {
                Mode = "web",
                Intent = "fresh_web",
                Reason = "问题涉及最新、实时、天气、版本、新闻、价格或政策信息，需要联网",
                Confidence = 0.86
            };
        }

        if (LooksLikeCreativeOrLearningQuestion(query))
        {
            return new AiChatRoute
            {
                Mode = "normal",
                Intent = "learning_or_creation",
                Reason = "这是学习、写作、代码或创意类问题，普通聊天会更自然",
                Confidence = 0.84
            };
        }

        return new AiChatRoute
        {
            Mode = "normal",
            Intent = "general",
            Reason = "问题不依赖实时资料，直接用普通聊天回答更稳定",
            Confidence = 0.78
        };
    }

    private static AiChatRoute BuildManualRoute(string mode, AiChatRouteRequest request)
    {
        if (mode == "rag" && request.KbId.GetValueOrDefault() <= 0)
        {
            return new AiChatRoute
            {
                Mode = "normal",
                Intent = "fallback",
                Reason = "手动选择了知识库问答，但没有可用知识库，已切换为普通聊天",
                Confidence = 0.95
            };
        }

        return new AiChatRoute
        {
            Mode = mode,
            KbId = mode == "rag" ? request.KbId : null,
            Intent = mode,
            Confidence = 1,
            Reason = mode switch
            {
                "web" => "用户手动选择联网搜索",
                "rag" => "用户手动选择知识库问答",
                "article" => "用户手动围绕文章提问",
                _ => "用户手动选择普通聊天"
            }
        };
    }

    private static string NormalizeMode(string? mode)
    {
        var value = (mode ?? "normal").Trim().ToLowerInvariant();
        return value is "auto" or "normal" or "web" or "rag" or "article" ? value : "normal";
    }

    private static bool NeedsFreshWebData(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return false;

        var asksForSearch = Regex.IsMatch(query,
            "联网|上网|网上|搜索|搜一下|查一下|查查|查找|检索|官网|官方|来源|引用|资料",
            RegexOptions.IgnoreCase);
        var fresh = Regex.IsMatch(query,
            "今天|今日|现在|当前|最近|最新|最新版|版本号|刚刚|实时|热点|新闻|快讯|价格|股价|汇率|天气|气温|温度|下雨|政策|法规|比赛|赛程|榜单|排名|发布|更新|latest|current|news|weather|price|version|release|trending",
            RegexOptions.IgnoreCase);
        var packageOrProductVersion = Regex.IsMatch(query,
            "(Django|FastAPI|Vue|React|Angular|Node|npm|pnpm|Python|Java|Spring|\\.NET|dotnet|MySQL|Redis|Docker|Kubernetes|K8s|TypeScript|Vite).*(最新|版本|release|latest)|" +
            "(最新|版本|release|latest).*(Django|FastAPI|Vue|React|Angular|Node|npm|pnpm|Python|Java|Spring|\\.NET|dotnet|MySQL|Redis|Docker|Kubernetes|K8s|TypeScript|Vite)",
            RegexOptions.IgnoreCase);

        return asksForSearch || fresh || packageOrProductVersion;
    }

    private static bool IsKnowledgeQuestion(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return false;

        return Regex.IsMatch(query,
            "这篇|本文|文章|博客|知识库|站内|根据.*资料|基于.*资料|引用来源|来源|总结.*重点|讲了什么|归纳.*内容|推荐.*文章|相关文章|作者|项目文档|文档里|教程里|博客里",
            RegexOptions.IgnoreCase);
    }

    private static bool LooksLikeArticleQuestion(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return false;

        return Regex.IsMatch(query,
            "这篇|本文|文章|总结|重点|术语|学习路线|面试题|下一篇|解释|读完|怎么看|核心观点",
            RegexOptions.IgnoreCase);
    }

    private static bool LooksLikeCreativeOrLearningQuestion(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return false;

        return Regex.IsMatch(query,
            "怎么学|如何学习|学习路线|解释|原理|写一段|帮我写|润色|翻译|总结|生成|代码|示例|笑话|故事|方案|计划|建议|面试题|表格|对比|优缺点",
            RegexOptions.IgnoreCase);
    }
}
