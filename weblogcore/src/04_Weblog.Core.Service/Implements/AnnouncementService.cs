using SqlSugar;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Service.Implements;

public class AnnouncementService : IAnnouncementService
{
    private readonly DbContext _dbContext;

    public AnnouncementService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AnnouncementDto> GetAsync()
    {
        var announcement = await _dbContext.Db.Queryable<Announcement>()
            .Where(it => !it.IsDeleted)
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .FirstAsync();

        if (announcement == null)
        {
            return new AnnouncementDto();
        }

        return new AnnouncementDto
        {
            Id = announcement.Id,
            Content = announcement.Content,
            IsEnabled = announcement.IsEnabled,
            CreateTime = announcement.CreateTime,
            UpdateTime = announcement.UpdateTime
        };
    }

    public async Task<AnnouncementDto> CreateAsync(CreateAnnouncementRequest request)
    {
        var announcement = new Announcement
        {
            Content = request.Content,
            IsEnabled = request.IsEnabled,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };

        // 如果要启用新的公告，先禁用其他公告
        if (request.IsEnabled)
        {
            await _dbContext.Db.Updateable<Announcement>()
                .SetColumns(it => it.IsEnabled == false)
                .Where(it => !it.IsDeleted)
                .ExecuteCommandAsync();
        }

        var id = await _dbContext.Db.Insertable(announcement).ExecuteReturnIdentityAsync();
        announcement.Id = id;

        return new AnnouncementDto
        {
            Id = announcement.Id,
            Content = announcement.Content,
            IsEnabled = announcement.IsEnabled,
            CreateTime = announcement.CreateTime,
            UpdateTime = announcement.UpdateTime
        };
    }

    public async Task<AnnouncementDto> UpdateAsync(UpdateAnnouncementRequest request)
    {
        var announcement = await _dbContext.Db.Queryable<Announcement>()
            .Where(it => it.Id == request.Id && !it.IsDeleted)
            .FirstAsync();

        if (announcement == null)
        {
            throw new Exception("公告不存在");
        }

        announcement.Content = request.Content;
        announcement.IsEnabled = request.IsEnabled;
        announcement.UpdateTime = DateTime.Now;

        // 如果要启用这条公告，先禁用其他公告
        if (request.IsEnabled)
        {
            await _dbContext.Db.Updateable<Announcement>()
                .SetColumns(it => it.IsEnabled == false)
                .Where(it => it.Id != request.Id && !it.IsDeleted)
                .ExecuteCommandAsync();
        }

        await _dbContext.Db.Updateable(announcement).ExecuteCommandAsync();

        return new AnnouncementDto
        {
            Id = announcement.Id,
            Content = announcement.Content,
            IsEnabled = announcement.IsEnabled,
            CreateTime = announcement.CreateTime,
            UpdateTime = announcement.UpdateTime
        };
    }

    public async Task<(List<AnnouncementDto> list, long total)> QueryAsync(AnnouncementQueryRequest request)
    {
        var query = _dbContext.Db.Queryable<Announcement>()
            .Where(it => !it.IsDeleted);

        if (!string.IsNullOrEmpty(request.Content))
        {
            query = query.Where(it => it.Content.Contains(request.Content));
        }

        if (request.IsEnabled.HasValue)
        {
            query = query.Where(it => it.IsEnabled == request.IsEnabled.Value);
        }

        if (request.StartDate.HasValue)
        {
            query = query.Where(it => it.CreateTime >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(it => it.CreateTime <= request.EndDate.Value);
        }

        var total = await query.CountAsync();

        var list = await query
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .Skip((request.PageNum - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(it => new AnnouncementDto
            {
                Id = it.Id,
                Content = it.Content,
                IsEnabled = it.IsEnabled,
                CreateTime = it.CreateTime,
                UpdateTime = it.UpdateTime
            })
            .ToListAsync();

        return (list, total);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var result = await _dbContext.Db.Updateable<Announcement>()
            .SetColumns(it => it.IsDeleted == true)
            .Where(it => it.Id == id)
            .ExecuteCommandAsync();

        return result > 0;
    }
}
