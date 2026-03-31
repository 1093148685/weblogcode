using SqlSugar;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;
using System.Text;
using System.Text.Json;

namespace Weblog.Core.Service.Implements;

public class AiSummaryService : IAiSummaryService
{
    private readonly DbContext _dbContext;
    private readonly IAiModelService _aiModelService;

    public AiSummaryService(DbContext dbContext, IAiModelService aiModelService)
    {
        _dbContext = dbContext;
        _aiModelService = aiModelService;
    }

    public async Task<AiSummaryDto?> GetByArticleIdAsync(long articleId)
    {
        var summary = await _dbContext.Db.Queryable<AiSummary>()
            .Where(it => it.ArticleId == articleId && it.IsEnabled)
            .FirstAsync();

        if (summary == null)
        {
            return null;
        }

        return new AiSummaryDto
        {
            Id = summary.Id,
            ArticleId = summary.ArticleId,
            Content = summary.Content,
            IsEnabled = summary.IsEnabled,
            CreateTime = summary.CreateTime,
            UpdateTime = summary.UpdateTime
        };
    }

    public async Task<AiSummaryDto> CreateAsync(CreateAiSummaryRequest request)
    {
        // 检查是否已存在
        var existing = await _dbContext.Db.Queryable<AiSummary>()
            .Where(it => it.ArticleId == request.ArticleId)
            .FirstAsync();

        if (existing != null)
        {
            existing.Content = request.Content;
            existing.IsEnabled = request.IsEnabled;
            existing.UpdateTime = DateTime.Now;
            await _dbContext.Db.Updateable(existing).ExecuteCommandAsync();
            return new AiSummaryDto
            {
                Id = existing.Id,
                ArticleId = existing.ArticleId,
                Content = existing.Content,
                IsEnabled = existing.IsEnabled,
                CreateTime = existing.CreateTime,
                UpdateTime = existing.UpdateTime
            };
        }

        var summary = new AiSummary
        {
            ArticleId = request.ArticleId,
            Content = request.Content,
            IsEnabled = request.IsEnabled,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };

        var id = await _dbContext.Db.Insertable(summary).ExecuteReturnIdentityAsync();
        summary.Id = id;

        return new AiSummaryDto
        {
            Id = summary.Id,
            ArticleId = summary.ArticleId,
            Content = summary.Content,
            IsEnabled = summary.IsEnabled,
            CreateTime = summary.CreateTime,
            UpdateTime = summary.UpdateTime
        };
    }

    public async Task<AiSummaryDto> UpdateAsync(UpdateAiSummaryRequest request)
    {
        var summary = await _dbContext.Db.Queryable<AiSummary>()
            .Where(it => it.Id == request.Id)
            .FirstAsync();

        if (summary == null)
        {
            throw new Exception("AI摘要不存在");
        }

        summary.Content = request.Content;
        summary.IsEnabled = request.IsEnabled;
        summary.UpdateTime = DateTime.Now;

        await _dbContext.Db.Updateable(summary).ExecuteCommandAsync();

        return new AiSummaryDto
        {
            Id = summary.Id,
            ArticleId = summary.ArticleId,
            Content = summary.Content,
            IsEnabled = summary.IsEnabled,
            CreateTime = summary.CreateTime,
            UpdateTime = summary.UpdateTime
        };
    }

    public async Task<string> GenerateSummaryAsync(long articleId)
    {
        try
        {
            // 获取文章内容
            var article = await _dbContext.ArticleDb
                .Where(it => it.Id == articleId)
                .FirstAsync();

            var articleContent = await _dbContext.ArticleContentDb
                .FirstAsync(it => it.ArticleId == articleId);

            if (article == null)
            {
                throw new Exception("文章不存在");
            }

            // 优先使用 ArticleContent 表的内容，其次使用 Summary
            var content = "";
            if (articleContent != null && !string.IsNullOrWhiteSpace(articleContent.Content))
            {
                content = articleContent.Content;
            }
            else if (!string.IsNullOrWhiteSpace(article.Summary))
            {
                content = article.Summary;
            }

            Console.WriteLine($"生成AI摘要 - 文章ID: {articleId}, 标题: {article.Title}, 内容长度: {content.Length}");

            // 如果内容为空，抛出异常
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new Exception("文章内容为空，无法生成摘要。请在编辑文章时填写正文或摘要。");
            }
            
            Console.WriteLine($"生成AI摘要 - 文章ID: {articleId}, 标题: {article.Title}, 内容长度: {content.Length}, 内容预览: {content.Substring(0, Math.Min(100, content.Length))}");

            // 获取默认的AI模型
            var aiModel = await _aiModelService.GetDefaultAsync();
            
            string summary;
            
            if (aiModel != null && !string.IsNullOrEmpty(aiModel.ApiKey))
            {
                // 使用配置的AI模型生成摘要
                summary = await CallAiApi(aiModel, article.Title, content);
            }
            else
            {
                // 没有配置AI模型，使用模拟摘要
                summary = $"这是一篇关于{article.Title}的文章。" +
                          $"本文主要介绍了{article.Title}的相关内容。" +
                          $"通过阅读本文，您可以了解{article.Title}的核心要点。" +
                          $"文章内容丰富，结构清晰，适合想要了解该主题的读者。";
            }

            // 保存或更新摘要
            var existing = await _dbContext.Db.Queryable<AiSummary>()
                .Where(it => it.ArticleId == articleId)
                .FirstAsync();

            if (existing != null)
            {
                existing.Content = summary;
                existing.IsEnabled = true;
                existing.UpdateTime = DateTime.Now;
                await _dbContext.Db.Updateable(existing).ExecuteCommandAsync();
            }
            else
            {
                var newSummary = new AiSummary
                {
                    ArticleId = articleId,
                    Content = summary,
                    IsEnabled = true,
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };
                await _dbContext.Db.Insertable(newSummary).ExecuteCommandAsync();
            }

            return summary;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"生成AI摘要失败: {ex.Message}");
            throw;
        }
    }

    private async Task<string> CallAiApi(AiModelDto model, string title, string content)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(120);

            // 截取内容前8000字符，以便生成更全面的摘要
            var truncatedContent = content.Length > 8000 ? content.Substring(0, 8000) : content;
            
            var prompt = $@"请为以下文章生成一个详细的摘要（200-300字），要求：
1. 概括文章的核心主题和主要内容
2. 提及文章的主要观点或技术要点
3. 让读者一眼就能了解这篇文章讲什么
4. 语言简洁明了，重点突出

标题：{title}

内容：{truncatedContent}";

            string requestBody = "";
            string endpoint = "";

            switch (model.Type.ToLower())
            {
                case "openai":
                case "minimax":
                    requestBody = JsonSerializer.Serialize(new
                    {
                        model = model.Model,
                        messages = new[]
                        {
                            new { role = "user", content = prompt }
                        },
                        max_tokens = 500
                    });
                    endpoint = $"{model.ApiUrl.TrimEnd('/')}/chat/completions";
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {model.ApiKey}");
                    break;

                case "claude":
                    requestBody = JsonSerializer.Serialize(new
                    {
                        model = model.Model,
                        messages = new[]
                        {
                            new { role = "user", content = prompt }
                        },
                        max_tokens = 500
                    });
                    endpoint = $"{model.ApiUrl.TrimEnd('/')}/messages";
                    httpClient.DefaultRequestHeaders.Add("x-api-key", model.ApiKey);
                    httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                    break;

                case "azure":
                    requestBody = JsonSerializer.Serialize(new
                    {
                        messages = new[]
                        {
                            new { role = "user", content = prompt }
                        },
                        max_tokens = 500
                    });
                    endpoint = $"{model.ApiUrl.TrimEnd('/')}/chat/completions?api-version=2024-02-15-preview";
                    httpClient.DefaultRequestHeaders.Add("api-key", model.ApiKey);
                    break;

                default:
                    // 其他类型使用 OpenAI 格式
                    requestBody = JsonSerializer.Serialize(new
                    {
                        model = model.Model,
                        messages = new[]
                        {
                            new { role = "user", content = prompt }
                        },
                        max_tokens = 500
                    });
                    endpoint = $"{model.ApiUrl.TrimEnd('/')}/chat/completions";
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {model.ApiKey}");
                    break;
            }

            var httpContent = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(endpoint, httpContent);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"AI API 响应状态: {response.StatusCode}");
            Console.WriteLine($"AI API 响应内容: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}");

            if (response.IsSuccessStatusCode)
            {
                var jsonDoc = JsonDocument.Parse(responseContent);
                
                // 尝试解析不同的响应格式
                string result = "";
                
                // OpenAI/MiniMax 格式
                if (jsonDoc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    result = choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                }
                // Claude 格式
                else if (jsonDoc.RootElement.TryGetProperty("content", out var contentArr))
                {
                    result = contentArr[0].GetProperty("text").GetString() ?? "";
                }
                
                if (!string.IsNullOrEmpty(result))
                {
                    Console.WriteLine($"AI 生成的摘要: {result}");
                    return result;
                }
            }
            
            // 如果API调用失败，返回默认摘要
            Console.WriteLine($"AI API 调用失败，使用默认摘要");
            return $"这是一篇关于{title}的文章。文章内容丰富，包含详细的技术讲解和实践案例。";
        }
        catch (Exception ex)
        {
            // 如果出错，返回默认摘要
            Console.WriteLine($"AI API 异常: {ex.Message}");
            return $"这是一篇关于{title}的文章。文章内容丰富，包含详细的技术讲解和实践案例。";
        }
    }

    private string _lastSummary = "";

    public async Task GenerateSummaryStreamAsync(AiModelDto model, string title, string content, Func<string, Task> onChunk)
    {
        _lastSummary = "";
        
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(120);

            var truncatedContent = content.Length > 8000 ? content.Substring(0, 8000) : content;
            
            var prompt = $@"请为以下文章生成一个详细的摘要（200-300字），要求：
1. 概括文章的核心主题和主要内容
2. 提及文章的主要观点或技术要点
3. 让读者一眼就能了解这篇文章讲什么
4. 语言简洁明了，重点突出
5. 直接输出摘要内容，不需要任何前缀

标题：{title}

内容：{truncatedContent}";

            string requestBody = "";
            string endpoint = "";

            switch (model.Type.ToLower())
            {
                case "openai":
                case "minimax":
                    requestBody = JsonSerializer.Serialize(new
                    {
                        model = model.Model,
                        messages = new[]
                        {
                            new { role = "user", content = prompt }
                        },
                        max_tokens = 500,
                        stream = true
                    });
                    endpoint = $"{model.ApiUrl.TrimEnd('/')}/chat/completions";
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {model.ApiKey}");
                    break;

                case "claude":
                    requestBody = JsonSerializer.Serialize(new
                    {
                        model = model.Model,
                        messages = new[]
                        {
                            new { role = "user", content = prompt }
                        },
                        max_tokens = 500,
                        stream = true
                    });
                    endpoint = $"{model.ApiUrl.TrimEnd('/')}/messages";
                    httpClient.DefaultRequestHeaders.Add("x-api-key", model.ApiKey);
                    httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                    break;

                default:
                    requestBody = JsonSerializer.Serialize(new
                    {
                        model = model.Model,
                        messages = new[]
                        {
                            new { role = "user", content = prompt }
                        },
                        max_tokens = 500,
                        stream = true
                    });
                    endpoint = $"{model.ApiUrl.TrimEnd('/')}/chat/completions";
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {model.ApiKey}");
                    break;
            }

            var httpContent = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(endpoint, httpContent);

            if (response.IsSuccessStatusCode)
            {
                var contentType = response.Content.Headers.ContentType?.MediaType;
                
                // 检查是否支持流式
                if (contentType != null && contentType.Contains("text/event-stream"))
                {
                    // 真正的流式响应
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var reader = new StreamReader(stream);

                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line != null && line.StartsWith("data: "))
                        {
                            var data = line.Substring(6);
                            if (data == "[DONE]")
                            {
                                break;
                            }

                            try
                            {
                                var jsonDoc = JsonDocument.Parse(data);
                                string contentChunk = "";

                                if (jsonDoc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                                {
                                    var delta = choices[0].GetProperty("delta");
                                    if (delta.TryGetProperty("content", out var contentElement))
                                    {
                                        contentChunk = contentElement.GetString() ?? "";
                                    }
                                }

                                if (!string.IsNullOrEmpty(contentChunk))
                                {
                                    _lastSummary += contentChunk;
                                    await onChunk(contentChunk);
                                }
                            }
                            catch
                            {
                                // 忽略解析错误
                            }
                        }
                    }
                }
                else
                {
                    // 非流式响应，一次性返回
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(responseContent);
                        string fullContent = "";
                        
                        if (jsonDoc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                        {
                            fullContent = choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                        }
                        
                        // 一次性发送所有内容，前端负责打字机效果
                        _lastSummary = fullContent;
                        await onChunk(fullContent);
                    }
                    catch
                    {
                        await onChunk("生成出错，请重试");
                    }
                }
            }
            else
            {
                await onChunk($"生成出错: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            await onChunk($"生成出错: {ex.Message}");
        }
    }

    public async Task SaveSummaryAsync(long articleId)
    {
        if (string.IsNullOrEmpty(_lastSummary))
        {
            return;
        }

        var existing = await _dbContext.Db.Queryable<AiSummary>()
            .Where(it => it.ArticleId == articleId)
            .FirstAsync();

        if (existing != null)
        {
            existing.Content = _lastSummary;
            existing.IsEnabled = true;
            existing.UpdateTime = DateTime.Now;
            await _dbContext.Db.Updateable(existing).ExecuteCommandAsync();
        }
        else
        {
            var newSummary = new AiSummary
                {
                    ArticleId = articleId,
                    Content = _lastSummary,
                    IsEnabled = true,
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };
                await _dbContext.Db.Insertable(newSummary).ExecuteCommandAsync();
        }
    }

    private string _lastContent = "";

    public async Task GenerateContentStreamAsync(AiModelDto model, string systemPrompt, string userPrompt, List<ConversationMessage>? conversations, Func<string, Task> onChunk)
    {
        _lastContent = "";
        
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(180);

            string requestBody = "";
            string endpoint = "";

            switch (model.Type.ToLower())
            {
                case "openai":
                case "minimax":
                    var openaiMessages = new List<object>();
                    openaiMessages.Add(new { role = "system", content = systemPrompt });
                    if (conversations != null && conversations.Count > 0)
                    {
                        foreach (var conv in conversations)
                        {
                            openaiMessages.Add(new { role = conv.Role, content = conv.Content });
                        }
                    }
                    openaiMessages.Add(new { role = "user", content = userPrompt });
                    
                    requestBody = JsonSerializer.Serialize(new
                    {
                        model = model.Model,
                        messages = openaiMessages.ToArray(),
                        max_tokens = 4000,
                        stream = true
                    });
                    endpoint = $"{model.ApiUrl.TrimEnd('/')}/chat/completions";
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {model.ApiKey}");
                    break;

                case "claude":
                    var claudeContent = systemPrompt + "\n\n";
                    if (conversations != null && conversations.Count > 0)
                    {
                        foreach (var conv in conversations)
                        {
                            claudeContent += $"【{conv.Role}】: {conv.Content}\n\n";
                        }
                    }
                    claudeContent += $"【user】: {userPrompt}";
                    
                    requestBody = JsonSerializer.Serialize(new
                    {
                        model = model.Model,
                        messages = new[]
                        {
                            new { role = "user", content = claudeContent }
                        },
                        max_tokens = 4000,
                        stream = true
                    });
                    endpoint = $"{model.ApiUrl.TrimEnd('/')}/messages";
                    httpClient.DefaultRequestHeaders.Add("x-api-key", model.ApiKey);
                    httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                    break;

                default:
                    var defaultMessages = new List<object>();
                    defaultMessages.Add(new { role = "system", content = systemPrompt });
                    if (conversations != null && conversations.Count > 0)
                    {
                        foreach (var conv in conversations)
                        {
                            defaultMessages.Add(new { role = conv.Role, content = conv.Content });
                        }
                    }
                    defaultMessages.Add(new { role = "user", content = userPrompt });
                    
                    requestBody = JsonSerializer.Serialize(new
                    {
                        model = model.Model,
                        messages = defaultMessages.ToArray(),
                        max_tokens = 4000,
                        stream = true
                    });
                    endpoint = $"{model.ApiUrl.TrimEnd('/')}/chat/completions";
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {model.ApiKey}");
                    break;
            }

            var httpContent = new StringContent(requestBody, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(endpoint, httpContent);

            if (response.IsSuccessStatusCode)
            {
                var contentType = response.Content.Headers.ContentType?.MediaType;
                
                if (contentType != null && contentType.Contains("text/event-stream"))
                {
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var reader = new StreamReader(stream);

                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line != null && line.StartsWith("data: "))
                        {
                            var data = line.Substring(6);
                            if (data == "[DONE]")
                            {
                                break;
                            }

                            try
                            {
                                var jsonDoc = JsonDocument.Parse(data);
                                string contentChunk = "";

                                if (jsonDoc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                                {
                                    var delta = choices[0].GetProperty("delta");
                                    if (delta.TryGetProperty("content", out var contentElement))
                                    {
                                        contentChunk = contentElement.GetString() ?? "";
                                    }
                                }

                                if (!string.IsNullOrEmpty(contentChunk))
                                {
                                    _lastContent += contentChunk;
                                    await onChunk(contentChunk);
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(responseContent);
                        string fullContent = "";
                        
                        if (jsonDoc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                        {
                            fullContent = choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                        }
                        
                        _lastContent = fullContent;
                        await onChunk(fullContent);
                    }
                    catch
                    {
                        await onChunk("生成出错，请重试");
                    }
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                await onChunk($"生成出错: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            await onChunk($"生成出错: {ex.Message}");
        }
    }
}
