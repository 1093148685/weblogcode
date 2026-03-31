using Mapster;
using SqlSugar;
using ToolGood.Words;
using Weblog.Core.Common.Helpers;
using Weblog.Core.Common.Utils;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Service.Implements;

public class CommentService : ICommentService
{
    private readonly DbContext _dbContext;
    private readonly IBlogSettingsService _blogSettingsService;
    private readonly IEmailService _emailService;
    private static WordsSearch? _wordsSearch;

    public CommentService(DbContext dbContext, IBlogSettingsService blogSettingsService, IEmailService emailService)
    {
        _dbContext = dbContext;
        _blogSettingsService = blogSettingsService;
        _emailService = emailService;
        LoadSensitiveWords();
    }

    private void LoadSensitiveWords()
    {
        if (_wordsSearch != null) return;

        try
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "sensitive_words.txt");
            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath);
                var sensitiveWords = lines
                    .Select(line => line.Trim())
                    .Where(word => !string.IsNullOrEmpty(word))
                    .ToList();

                _wordsSearch = new WordsSearch();
                _wordsSearch.SetKeywords(sensitiveWords);
            }
        }
        catch
        {
            // 如果加载失败，使用空集合
        }
    }

    private async Task<(bool hasSensitive, string? reason)> CheckSensitiveWord(string text)
    {
        if (string.IsNullOrEmpty(text))
            return (false, null);

        var settings = await _blogSettingsService.GetAsync();
        if (!settings.IsCommentSensiWordOpen || _wordsSearch == null)
            return (false, null);

        if (_wordsSearch.ContainsAny(text))
        {
            var results = _wordsSearch.FindAll(text);
            var keywords = results.Select(r => r.Keyword).Distinct().ToList();
            var reason = $"包含敏感词：{string.Join(", ", keywords)}";
            return (true, reason);
        }

        return (false, null);
    }

    public async Task<CommentDto> CreateAsync(CreateCommentRequest request)
    {
        // 检查是否开启了评论审核
        var settings = await _blogSettingsService.GetAsync();
        int initialStatus = settings.IsCommentExamineOpen ? 1 : 2; // 1=待审核，2=已通过
        string? reason = null;

        // 检查敏感词过滤（使用 DFA 算法）
        var (hasSensitiveContent, reasonContent) = await CheckSensitiveWord(request.Content);
        var (hasSensitiveNickname, reasonNickname) = await CheckSensitiveWord(request.Nickname);

        if (hasSensitiveContent || hasSensitiveNickname)
        {
            // 包含敏感词，标记为审核不通过
            initialStatus = 3;
            reason = reasonContent ?? reasonNickname;
        }

        // 如果是回复评论，查找被回复评论的昵称
        string? replyNickname = null;
        if (request.ReplyCommentId.HasValue && request.ReplyCommentId.Value > 0)
        {
            var parentComment = await _dbContext.CommentDb
                .Where(it => it.Id == request.ReplyCommentId.Value && !it.IsDeleted)
                .FirstAsync();
            if (parentComment != null)
            {
                replyNickname = parentComment.Nickname;
            }
        }

        var comment = new Comment
        {
            Content = request.Content,
            Avatar = request.Avatar,
            Nickname = request.Nickname,
            Mail = request.Mail,
            Website = request.Website,
            RouterUrl = request.RouterUrl,
            ReplyCommentId = request.ReplyCommentId ?? 0,
            ParentCommentId = request.ParentCommentId ?? 0,
            Status = initialStatus,
            Reason = reason,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now,
            IpAddress = request.IpAddress,
            IpLocation = request.IpLocation,
            DeviceType = request.DeviceType,
            Browser = request.Browser,
            ReplyNickname = replyNickname,
            Images = request.Images,
            IsSecret = request.IsSecret,
            SecretContent = request.IsSecret && !string.IsNullOrEmpty(request.SecretContent) 
                ? AesUtil.Encrypt(request.SecretContent, AesUtil.DeriveKey(request.SecretKey ?? "")) 
                : null,
            SecretKeyHash = request.IsSecret && !string.IsNullOrEmpty(request.SecretKey) 
                ? AesUtil.HashSha256(request.SecretKey) 
                : null,
            ExpiresAt = request.IsSecret ? request.ExpiresAt : null,
            IsExpired = false,
            IsReset = false,
            IsAdmin = request.IsSecret
        };

        var id = await _dbContext.Db.Insertable(comment).ExecuteReturnIdentityAsync();
        comment.Id = id;

        // 发送邮件通知（仅当评论审核通过时）
        if (initialStatus == 2)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var settings = await _blogSettingsService.GetAsync();
                    if (settings.IsEmailNotificationOpen)
                    {
                        await _emailService.SendCommentNotificationToAdminAsync(comment, comment.ReplyNickname);

                        if (comment.ReplyCommentId.HasValue && comment.ReplyCommentId.Value > 0)
                        {
                            var parentComment = await _dbContext.CommentDb
                                .Where(it => it.Id == comment.ReplyCommentId.Value && !it.IsDeleted)
                                .FirstAsync();
                            if (parentComment != null && !string.IsNullOrEmpty(parentComment.Mail))
                            {
                                await _emailService.SendReplyNotificationToUserAsync(comment, parentComment);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"发送邮件通知失败: {ex.Message}");
                }
            });
        }

        // 构建返回结果
        var result = comment.Adapt<CommentDto>();
        
        // 设置提示消息
        if (initialStatus == 3)
        {
            // 包含敏感词，审核不通过
            result.Message = "评论内容中包含敏感词，请重新编辑后再提交";
        }
        else if (initialStatus == 1)
        {
            // 待审核
            result.Message = "评论已提交, 等待博主审核通过";
        }

        return result;
    }

    public async Task<CommentDto> UpdateAsync(UpdateCommentRequest request)
    {
        var comment = await _dbContext.CommentDb
            .Where(it => it.Id == request.Id && !it.IsDeleted)
            .FirstAsync();
        if (comment == null)
        {
            throw new Exception("评论不存在");
        }

        comment.Content = request.Content;
        comment.Status = request.Status;
        comment.Reason = request.Reason;
        comment.UpdateTime = DateTime.Now;

        await _dbContext.Db.Updateable(comment).ExecuteCommandAsync();

        return await GetByIdAsync(comment.Id);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var comment = await _dbContext.CommentDb
            .Where(it => it.Id == id && !it.IsDeleted)
            .FirstAsync();
        if (comment == null)
        {
            return false;
        }

        comment.IsDeleted = true;
        comment.UpdateTime = DateTime.Now;
        return await _dbContext.Db.Updateable(comment).ExecuteCommandAsync() > 0;
    }

    public async Task<bool> BatchDeleteAsync(List<long> ids)
    {
        if (ids == null || ids.Count == 0)
            return false;

        var comments = await _dbContext.CommentDb
            .Where(it => ids.Contains(it.Id) && !it.IsDeleted)
            .ToListAsync();

        foreach (var comment in comments)
        {
            comment.IsDeleted = true;
            comment.UpdateTime = DateTime.Now;
        }

        return await _dbContext.Db.Updateable(comments).ExecuteCommandAsync() > 0;
    }

    public async Task<CommentDto> GetByIdAsync(long id)
    {
        var comment = await _dbContext.CommentDb.FirstAsync(it => it.Id == id);
        if (comment == null)
        {
            throw new Exception("评论不存在");
        }

        var dto = comment.Adapt<CommentDto>();
        
        return dto;
    }

    public async Task<PageDto<CommentDto>> GetAdminPageAsync(CommentPageRequest request)
    {
        var query = _dbContext.CommentDb.Where(it => !it.IsDeleted);

        // 按昵称模糊查询
        if (!string.IsNullOrWhiteSpace(request.Nickname))
        {
            query = query.Where(it => it.Nickname.Contains(request.Nickname));
        }

        // 按状态查询
        if (request.Status.HasValue)
        {
            query = query.Where(it => it.Status == request.Status);
        }

        // 按私密内容筛选
        if (request.IsSecret.HasValue)
        {
            query = query.Where(it => it.IsSecret == request.IsSecret.Value);
        }

        // 按日期范围查询
        if (!string.IsNullOrWhiteSpace(request.StartDate))
        {
            var startDate = DateTime.Parse(request.StartDate);
            query = query.Where(it => it.CreateTime >= startDate);
        }
        if (!string.IsNullOrWhiteSpace(request.EndDate))
        {
            var endDate = DateTime.Parse(request.EndDate).AddDays(1);
            query = query.Where(it => it.CreateTime < endDate);
        }

        var total = await query.CountAsync();
        var list = await query
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .Skip((request.PageNum - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PageDto<CommentDto>
        {
            List = list.Adapt<List<CommentDto>>(),
            Total = total,
            PageNum = request.PageNum,
            PageSize = request.PageSize
        };
    }

    public async Task<List<CommentDto>> GetPortalListAsync(string? routerUrl)
    {
        var query = _dbContext.CommentDb
            .Where(it => !it.IsDeleted && it.Status == 2); // 仅显示已通过的评论（2=正常）

        if (!string.IsNullOrEmpty(routerUrl))
        {
            query = query.Where(it => it.RouterUrl == routerUrl);
        }

        var list = await query
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .ToListAsync();

        // 手动映射到 DTO
        var dtos = list.Select(c => new CommentDto
        {
            Id = c.Id,
            Content = c.Content,
            Avatar = c.Avatar,
            Nickname = c.Nickname,
            Mail = c.Mail,
            Website = c.Website,
            RouterUrl = c.RouterUrl,
            ReplyCommentId = c.ReplyCommentId,
            ParentCommentId = c.ParentCommentId,
            Status = c.Status,
            Reason = c.Reason,
            CreateTime = c.CreateTime,
            UpdateTime = c.UpdateTime,
            ReplyNickname = c.ReplyNickname,
            IpAddress = c.IpAddress,
            IpLocation = c.IpLocation,
            DeviceType = c.DeviceType,
            Browser = c.Browser,
            FlowerCount = c.FlowerCount,
            Images = c.Images,
            IsSecret = c.IsSecret
        }).ToList();

        return BuildCommentTree(dtos);
    }

    private List<CommentDto> BuildCommentTree(List<CommentDto> comments)
    {
        var dict = comments.ToDictionary(c => c.Id);
        var roots = new List<CommentDto>();

        foreach (var comment in comments)
        {
            if (!comment.ParentCommentId.HasValue || comment.ParentCommentId.Value == 0)
            {
                roots.Add(comment);
            }
            else
            {
                // 优先使用 replyCommentId 来查找父评论，因为 replyCommentId 指向直接父评论
                var parentId = comment.ReplyCommentId > 0 ? comment.ReplyCommentId : comment.ParentCommentId;
                
                if (dict.TryGetValue(parentId.Value, out var parent))
                {
                    parent.Children ??= new List<CommentDto>();
                    parent.Children.Add(comment);
                }
                else if (dict.TryGetValue(comment.ParentCommentId.Value, out var rootParent))
                {
                    // 如果找不到直接父评论，则挂载到一级评论下面
                    rootParent.Children ??= new List<CommentDto>();
                    rootParent.Children.Add(comment);
                }
            }
        }

        return roots;
    }

    public async Task<bool> ApproveAsync(long id)
    {
        var comment = await _dbContext.CommentDb
            .Where(it => it.Id == id && !it.IsDeleted)
            .FirstAsync();
        if (comment == null)
        {
            return false;
        }

        comment.Status = 2; // 已通过（2=正常）
        comment.Reason = null;
        comment.UpdateTime = DateTime.Now;
        return await _dbContext.Db.Updateable(comment).ExecuteCommandAsync() > 0;
    }

    public async Task<bool> RejectAsync(long id, string reason)
    {
        var comment = await _dbContext.CommentDb
            .Where(it => it.Id == id && !it.IsDeleted)
            .FirstAsync();
        if (comment == null)
        {
            return false;
        }

        comment.Status = 3; // 未通过
        comment.Reason = reason;
        comment.UpdateTime = DateTime.Now;
        return await _dbContext.Db.Updateable(comment).ExecuteCommandAsync() > 0;
    }

    public async Task<bool> SendFlowerAsync(long commentId, string userKey)
    {
        // 检查是否已经送过花
        var existing = await _dbContext.Db.Queryable<FlowerRecord>()
            .Where(it => it.CommentId == commentId && it.UserKey == userKey)
            .FirstAsync();
        
        if (existing != null)
            return false; // 已经送过花了

        // 添加送花记录
        var record = new FlowerRecord
        {
            CommentId = commentId,
            UserKey = userKey,
            CreateTime = DateTime.Now
        };
        await _dbContext.Db.Insertable(record).ExecuteCommandAsync();

        // 更新评论的送花数量
        await _dbContext.Db.Updateable<Comment>()
            .SetColumns(it => it.FlowerCount == it.FlowerCount + 1)
            .Where(it => it.Id == commentId)
            .ExecuteCommandAsync();

        return true;
    }

    public async Task<bool> CancelFlowerAsync(long commentId, string userKey)
    {
        // 查找送花记录
        var record = await _dbContext.Db.Queryable<FlowerRecord>()
            .Where(it => it.CommentId == commentId && it.UserKey == userKey)
            .FirstAsync();
        
        if (record == null)
            return false; // 没有送花记录

        // 删除送花记录
        await _dbContext.Db.Deleteable<FlowerRecord>()
            .Where(it => it.Id == record.Id)
            .ExecuteCommandAsync();

        // 减少评论的送花数量
        await _dbContext.Db.Updateable<Comment>()
            .SetColumns(it => it.FlowerCount == it.FlowerCount - 1)
            .Where(it => it.Id == commentId && it.FlowerCount > 0)
            .ExecuteCommandAsync();

        return true;
    }

    public async Task<Dictionary<long, bool>> GetFlowerStatusAsync(List<long> commentIds, string userKey)
    {
        var result = new Dictionary<long, bool>();
        
        if (commentIds == null || commentIds.Count == 0 || string.IsNullOrWhiteSpace(userKey))
            return result;

        var records = await _dbContext.Db.Queryable<FlowerRecord>()
            .Where(it => commentIds.Contains(it.CommentId) && it.UserKey == userKey)
            .ToListAsync();

        foreach (var id in commentIds)
        {
            result[id] = records.Any(r => r.CommentId == id);
        }

        return result;
    }

    public Task<CaptchaResponse> GetCaptchaAsync()
    {
        var rand = new Random();
        var a = rand.Next(1, 10);
        var b = rand.Next(1, 10);
        var question = $"{a}+{b}=?";
        var answer = (a + b).ToString();
        var captchaId = CaptchaStore.Generate(answer);

        return Task.FromResult(new CaptchaResponse
        {
            CaptchaId = captchaId,
            Question = $"{a}+{b}=?"
        });
    }

    public async Task<SecretContentResponse> VerifySecretAsync(VerifySecretRequest request)
    {
        var (canTry, waitSeconds) = CaptchaStore.CanTry(request.CommentId.ToString());
        if (!canTry)
            throw new Exception($"验证失败次数过多，请等待 {waitSeconds} 秒后重试");

        var captcha = CaptchaStore.Get(request.CaptchaId);
        if (captcha == null || captcha.IsUsed)
            throw new Exception("验证码已过期，请重新获取");

        if (captcha.Answer != request.Captcha)
        {
            CaptchaStore.RecordFailedAttempt(request.CommentId.ToString());
            throw new Exception("验证码错误");
        }

        var comment = await _dbContext.CommentDb
            .Where(it => it.Id == request.CommentId && !it.IsDeleted && it.IsSecret)
            .FirstAsync();

        if (comment == null)
            throw new Exception("评论不存在或不是私密内容");

        if (comment.IsReset)
            throw new Exception("私密内容已被重置，无法查看");

        if (comment.ExpiresAt.HasValue && comment.ExpiresAt.Value < DateTime.Now)
        {
            comment.IsExpired = true;
            await _dbContext.Db.Updateable(comment).ExecuteCommandAsync();
            throw new Exception("私密内容已过期");
        }

        var keyHash = AesUtil.HashSha256(request.SecretKey);
        if (comment.SecretKeyHash != keyHash)
        {
            CaptchaStore.RecordFailedAttempt(request.CommentId.ToString());
            throw new Exception("密钥错误");
        }

        CaptchaStore.ClearFailedAttempt(request.CommentId.ToString());

        var decryptedContent = AesUtil.Decrypt(comment.SecretContent ?? "", AesUtil.DeriveKey(request.SecretKey));

        return new SecretContentResponse
        {
            Content = decryptedContent
        };
    }

    public async Task<bool> ResetSecretAsync(ResetSecretRequest request)
    {
        var comment = await _dbContext.CommentDb
            .Where(it => it.Id == request.CommentId && !it.IsDeleted && it.IsSecret)
            .FirstAsync();

        if (comment == null)
            throw new Exception("评论不存在或不是私密内容");

        comment.IsReset = true;
        comment.SecretContent = null;
        comment.SecretKeyHash = null;
        comment.ExpiresAt = null;

        if (!string.IsNullOrEmpty(request.NewSecretContent) && !string.IsNullOrEmpty(request.NewSecretKey))
        {
            comment.IsReset = false;
            comment.SecretContent = AesUtil.Encrypt(request.NewSecretContent, AesUtil.DeriveKey(request.NewSecretKey));
            comment.SecretKeyHash = AesUtil.HashSha256(request.NewSecretKey);
            comment.ExpiresAt = request.ExpiresAt;
        }

        comment.UpdateTime = DateTime.Now;
        return await _dbContext.Db.Updateable(comment).ExecuteCommandAsync() > 0;
    }
}
