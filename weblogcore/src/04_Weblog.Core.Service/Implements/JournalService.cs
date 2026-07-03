using Mapster;
using SqlSugar;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Service.Implements;

public class JournalService : IJournalService
{
    private readonly DbContext _dbContext;

    public JournalService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<JournalDto> CreateAsync(CreateJournalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new Exception("标题不能为空");

        var now = DateTime.Now;
        var journal = new Journal
        {
            Title = request.Title.Trim(),
            CreateTime = now,
            UpdateTime = now
        };

        var id = await _dbContext.Db.Insertable(journal).ExecuteReturnIdentityAsync();
        journal.Id = id;

        if (!string.IsNullOrWhiteSpace(request.Content))
        {
            var content = new JournalContent
            {
                JournalId = id,
                Content = request.Content
            };
            await _dbContext.Db.Insertable(content).ExecuteCommandAsync();
        }

        return await GetByIdAsync(id);
    }

    public async Task<JournalDto> UpdateAsync(UpdateJournalRequest request)
    {
        var journal = await _dbContext.Db.Queryable<Journal>()
            .Where(it => it.Id == request.Id && !it.IsDeleted)
            .FirstAsync();

        if (journal == null)
            throw new Exception("日志不存在");

        journal.Title = request.Title.Trim();
        journal.UpdateTime = DateTime.Now;

        await _dbContext.Db.Updateable(journal).ExecuteCommandAsync();

        // upsert content
        var existingContent = await _dbContext.Db.Queryable<JournalContent>()
            .FirstAsync(it => it.JournalId == request.Id);
        if (existingContent != null)
        {
            existingContent.Content = request.Content ?? string.Empty;
            await _dbContext.Db.Updateable(existingContent).ExecuteCommandAsync();
        }
        else
        {
            var content = new JournalContent
            {
                JournalId = request.Id,
                Content = request.Content ?? string.Empty
            };
            await _dbContext.Db.Insertable(content).ExecuteCommandAsync();
        }

        return await GetByIdAsync(request.Id);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var journal = await _dbContext.Db.Queryable<Journal>()
            .Where(it => it.Id == id && !it.IsDeleted)
            .FirstAsync();

        if (journal == null)
            return false;

        journal.IsDeleted = true;
        journal.UpdateTime = DateTime.Now;
        return await _dbContext.Db.Updateable(journal).ExecuteCommandAsync() > 0;
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
        dto.Content = content?.Content ?? string.Empty;
        return dto;
    }

    public async Task<PageDto<JournalAdminDto>> GetAdminPageAsync(PageRequest request)
    {
        var total = await _dbContext.Db.Queryable<Journal>()
            .Where(it => !it.IsDeleted)
            .CountAsync();

        var list = await _dbContext.Db.Queryable<Journal>()
            .Where(it => !it.IsDeleted)
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .Skip((request.PageNum - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PageDto<JournalAdminDto>
        {
            List = list.Adapt<List<JournalAdminDto>>(),
            Total = total,
            PageNum = request.PageNum,
            PageSize = request.PageSize
        };
    }
}
