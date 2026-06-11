using SqlSugar;
using Weblog.Core.Model.DTOs;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Service.Interfaces;

namespace Weblog.Core.Service.Implements;

public class AiModelService : IAiModelService
{
    private readonly DbContext _dbContext;

    public AiModelService(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AiModelDto>> GetAllAsync()
    {
        var list = await _dbContext.Db.Queryable<AiModel>()
            .OrderBy(it => it.IsDefault, OrderByType.Desc)
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .ToListAsync();

        return list.Select(it => new AiModelDto
        {
            Id = it.Id,
            Name = it.Name,
            Type = it.Type,
            ApiKey = it.ApiKey,
            ApiUrl = it.ApiUrl,
            Model = it.Model,
            IsDefault = it.IsDefault,
            IsEnabled = it.IsEnabled,
            Remark = it.Remark,
            CreateTime = it.CreateTime,
            UpdateTime = it.UpdateTime
        }).ToList();
    }

    public async Task<AiModelDto?> GetByIdAsync(long id)
    {
        var model = await _dbContext.Db.Queryable<AiModel>()
            .Where(it => it.Id == id)
            .FirstAsync();

        if (model == null) return null;

        return new AiModelDto
        {
            Id = model.Id,
            Name = model.Name,
            Type = model.Type,
            ApiKey = model.ApiKey,
            ApiUrl = model.ApiUrl,
            Model = model.Model,
            IsDefault = model.IsDefault,
            IsEnabled = model.IsEnabled,
            Remark = model.Remark,
            CreateTime = model.CreateTime,
            UpdateTime = model.UpdateTime
        };
    }

    public async Task<List<AiModelDto>> GetAllEnabledAsync()
    {
        var list = await _dbContext.Db.Queryable<AiModel>()
            .Where(it => it.IsEnabled)
            .OrderBy(it => it.IsDefault, OrderByType.Desc)
            .OrderBy(it => it.CreateTime, OrderByType.Desc)
            .ToListAsync();

        return list.Select(it => new AiModelDto
        {
            Id = it.Id,
            Name = it.Name,
            Type = it.Type,
            ApiKey = it.ApiKey,
            ApiUrl = it.ApiUrl,
            Model = it.Model,
            IsDefault = it.IsDefault,
            IsEnabled = it.IsEnabled,
            Remark = it.Remark,
            CreateTime = it.CreateTime,
            UpdateTime = it.UpdateTime
        }).ToList();
    }

    public async Task<AiModelDto?> GetByTypeAndModelAsync(string serviceType, string modelName)
    {
        var model = await _dbContext.Db.Queryable<AiModel>()
            .Where(it => it.IsEnabled 
                && it.Type.ToLower() == serviceType.ToLower() 
                && it.Model.ToLower() == modelName.ToLower())
            .FirstAsync();

        if (model == null) return null;

        return new AiModelDto
        {
            Id = model.Id,
            Name = model.Name,
            Type = model.Type,
            ApiKey = model.ApiKey,
            ApiUrl = model.ApiUrl,
            Model = model.Model,
            IsDefault = model.IsDefault,
            IsEnabled = model.IsEnabled,
            Remark = model.Remark,
            CreateTime = model.CreateTime,
            UpdateTime = model.UpdateTime
        };
    }

    public async Task<AiModelDto?> GetDefaultAsync()
    {
        var model = await _dbContext.Db.Queryable<AiModel>()
            .Where(it => it.IsEnabled)
            .OrderBy(it => it.IsDefault, OrderByType.Desc)
            .OrderBy(it => it.CreateTime, OrderByType.Asc)
            .FirstAsync();

        if (model == null) return null;

        return new AiModelDto
        {
            Id = model.Id,
            Name = model.Name,
            Type = model.Type,
            ApiKey = model.ApiKey,
            ApiUrl = model.ApiUrl,
            Model = model.Model,
            IsDefault = model.IsDefault,
            IsEnabled = model.IsEnabled,
            Remark = model.Remark,
            CreateTime = model.CreateTime,
            UpdateTime = model.UpdateTime
        };
    }

    public async Task<AiModelDto> CreateAsync(CreateAiModelRequest request)
    {
        // 如果设置为默认，先取消其他默认
        if (request.IsDefault)
        {
            await _dbContext.Db.Updateable<AiModel>()
                .SetColumns(it => it.IsDefault == false)
                .Where(it => it.IsDefault)
                .ExecuteCommandAsync();
        }

        var model = new AiModel
        {
            Name = request.Name,
            Type = request.Type,
            ApiKey = request.ApiKey,
            ApiUrl = request.ApiUrl,
            Model = request.Model,
            IsDefault = request.IsDefault,
            IsEnabled = request.IsEnabled,
            Remark = request.Remark,
            CreateTime = DateTime.Now,
            UpdateTime = DateTime.Now
        };

        var id = await _dbContext.Db.Insertable(model).ExecuteReturnIdentityAsync();
        model.Id = id;

        return new AiModelDto
        {
            Id = model.Id,
            Name = model.Name,
            Type = model.Type,
            ApiKey = model.ApiKey,
            ApiUrl = model.ApiUrl,
            Model = model.Model,
            IsDefault = model.IsDefault,
            IsEnabled = model.IsEnabled,
            Remark = model.Remark,
            CreateTime = model.CreateTime,
            UpdateTime = model.UpdateTime
        };
    }

    public async Task<AiModelDto> UpdateAsync(UpdateAiModelRequest request)
    {
        var model = await _dbContext.Db.Queryable<AiModel>()
            .Where(it => it.Id == request.Id)
            .FirstAsync();

        if (model == null)
        {
            throw new Exception("模型不存在");
        }

        // 如果设置为默认，先取消其他默认
        if (request.IsDefault && !model.IsDefault)
        {
            await _dbContext.Db.Updateable<AiModel>()
                .SetColumns(it => it.IsDefault == false)
                .Where(it => it.IsDefault && it.Id != request.Id)
                .ExecuteCommandAsync();
        }

        model.Name = request.Name;
        model.Type = request.Type;
        model.ApiKey = request.ApiKey;
        model.ApiUrl = request.ApiUrl;
        model.Model = request.Model;
        model.IsDefault = request.IsDefault;
        model.IsEnabled = request.IsEnabled;
        model.Remark = request.Remark;
        model.UpdateTime = DateTime.Now;

        await _dbContext.Db.Updateable(model).ExecuteCommandAsync();

        return new AiModelDto
        {
            Id = model.Id,
            Name = model.Name,
            Type = model.Type,
            ApiKey = model.ApiKey,
            ApiUrl = model.ApiUrl,
            Model = model.Model,
            IsDefault = model.IsDefault,
            IsEnabled = model.IsEnabled,
            Remark = model.Remark,
            CreateTime = model.CreateTime,
            UpdateTime = model.UpdateTime
        };
    }

    public async Task DeleteAsync(long id)
    {
        await _dbContext.Db.Deleteable<AiModel>()
            .Where(it => it.Id == id)
            .ExecuteCommandAsync();
    }
}
