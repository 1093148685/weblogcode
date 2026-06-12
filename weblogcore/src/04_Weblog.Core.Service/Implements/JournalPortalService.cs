using Mapster;
using Markdig;
using SqlSugar;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Service.Implements;

public class JournalPortalService : IJournalPortalService
{
    private readonly DbContext _dbContext;
    private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseSoftlineBreakAsHardlineBreak()
        .Build();

    public JournalPortalService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PageDto<JournalDto>> GetPageAsync(JournalPageRequest request)
    {
        var query = _dbContext.Db.Queryable<Journal>()
            .Where(it => !it.IsDeleted);

        // 按日期筛选
        if (!string.IsNullOrWhiteSpace(request.Date))
        {
            if (DateTime.TryParse(request.Date, out var date))
            {
                var nextDay = date.AddDays(1);
                query = query.Where(it => it.CreateTime >= date && it.CreateTime < nextDay);
            }
        }

        var total = await query.CountAsync();

        var list = await query
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .Skip((request.PageNum - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var journalIds = list.Select(it => it.Id).ToList();
        var contents = await _dbContext.Db.Queryable<JournalContent>()
            .Where(it => journalIds.Contains(it.JournalId))
            .ToListAsync();
        var contentDict = contents.ToDictionary(c => c.JournalId, c => c.Content);

        var result = list.Select(journal =>
        {
            var dto = journal.Adapt<JournalDto>();
            dto.Content = Markdown.ToHtml(contentDict.GetValueOrDefault(journal.Id, string.Empty), _pipeline);
            return dto;
        }).ToList();

        return new PageDto<JournalDto>
        {
            List = result,
            Total = total,
            PageNum = request.PageNum,
            PageSize = request.PageSize
        };
    }

    public async Task<JournalDto> GetByIdAsync(long id)
    {
        var journal = await _dbContext.Db.Queryable<Journal>()
            .Where(it => it.Id == id && !it.IsDeleted)
            .FirstAsync();

        if (journal == null)
            throw new Exception("日志不存在");

        var content = await _dbContext.Db.Queryable<JournalContent>()
            .FirstAsync(it => it.JournalId == id);

        var dto = journal.Adapt<JournalDto>();
        dto.Content = Markdown.ToHtml(content?.Content ?? string.Empty, _pipeline);
        return dto;
    }

    public async Task IncrementViewCountAsync(long id)
    {
        await _dbContext.Db.Updateable<Journal>()
            .SetColumns(it => it.ReadNum == it.ReadNum + 1)
            .Where(it => it.Id == id)
            .ExecuteCommandAsync();
    }

    public async Task<List<string>> GetDatesWithJournalsAsync()
    {
        var list = await _dbContext.Db.Queryable<Journal>()
            .Where(it => !it.IsDeleted)
            .Select(it => it.CreateTime)
            .ToListAsync();

        return list
            .Select(d => d.ToString("yyyy-MM-dd"))
            .Distinct()
            .ToList();
    }
}
