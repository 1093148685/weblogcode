using Weblog.Core.Model.DTOs;

namespace Weblog.Core.Service.Interfaces;

public interface IAiModelService
{
    Task<List<AiModelDto>> GetAllAsync();
    Task<List<AiModelDto>> GetAllEnabledAsync();
    Task<AiModelDto?> GetByIdAsync(long id);
    Task<AiModelDto?> GetByTypeAndModelAsync(string serviceType, string modelName);
    Task<AiModelDto?> GetDefaultAsync();
    Task<AiModelDto> CreateAsync(CreateAiModelRequest request);
    Task<AiModelDto> UpdateAsync(UpdateAiModelRequest request);
    Task DeleteAsync(long id);
}
