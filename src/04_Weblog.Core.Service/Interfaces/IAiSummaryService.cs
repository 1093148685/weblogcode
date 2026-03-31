using Weblog.Core.Model.DTOs;

namespace Weblog.Core.Service.Interfaces;

public interface IAiSummaryService
{
    Task<AiSummaryDto?> GetByArticleIdAsync(long articleId);
    Task<AiSummaryDto> CreateAsync(CreateAiSummaryRequest request);
    Task<AiSummaryDto> UpdateAsync(UpdateAiSummaryRequest request);
    Task<string> GenerateSummaryAsync(long articleId);
    Task GenerateSummaryStreamAsync(AiModelDto model, string title, string content, Func<string, Task> onChunk);
    Task SaveSummaryAsync(long articleId);
    Task GenerateContentStreamAsync(AiModelDto model, string systemPrompt, string userPrompt, List<ConversationMessage>? conversations, Func<string, Task> onChunk);
}
