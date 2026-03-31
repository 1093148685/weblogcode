using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SqlSugar;
using Weblog.Core.Api.Middleware;
using Weblog.Core.Api.Services;
using Weblog.Core.Model.Entities;
using Weblog.Core.Repository;
using Weblog.Core.Repository.UnitOfWork;
using Weblog.Core.Service.Implements;
using Weblog.Core.Service.Interfaces;
using Weblog.Core.Service.AI;
using Weblog.Core.Service.AI.Core;
using Weblog.Core.Service.AI.Providers;
using Weblog.Core.Service.AI.Plugins;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Port=3306;Database=your_database;User=root;Password=your_password;";

builder.Services.AddScoped<ISqlSugarClient>(s =>
{
    var db = new SqlSugarClient(new ConnectionConfig()
    {
        ConnectionString = connectionString,
        DbType = DbType.MySql,
        IsAutoCloseConnection = true,
        InitKeyType = InitKeyType.Attribute,
        MoreSettings = new ConnMoreSettings()
        {
            IsAutoRemoveDataCache = true
        }
    });

    db.CodeFirst.InitTables(
        typeof(SysUser),
        typeof(UserRole),
        typeof(Category),
        typeof(Tag),
        typeof(Article),
        typeof(ArticleTag),
        typeof(ArticleContent),
        typeof(ArticleCategoryRel),
        typeof(Comment),
        typeof(Wiki),
        typeof(WikiCatalog),
        typeof(BlogSettings),
        typeof(Statistics),
        typeof(StatisticsArticlePv),
        typeof(Announcement),
        typeof(AiSummary),
        typeof(AiModel),
        typeof(AiProvider),
        typeof(AiPlugin),
        typeof(AiConversation),
        typeof(AiUsageLog),
        typeof(AiAgentLog),
        typeof(AiAgentConfig)
    );

    return db;
});

builder.Services.AddScoped<DbContext>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddSingleton<MinIOService>();
builder.Services.AddSingleton<InMemoryCacheService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IArticlePortalService, ArticlePortalService>();
builder.Services.AddScoped<IBlogSettingsService, BlogSettingsService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ICommentAdminService, CommentAdminService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IWikiService, WikiService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IAiSummaryService, AiSummaryService>();
builder.Services.AddScoped<IAiModelService, AiModelService>();
builder.Services.AddScoped<IStickerService, StickerService>();

var aesKey = builder.Configuration["AiEncryption:AesKey"] ?? "WeblogCoreAesKey2024!@#$";
builder.Services.AddSingleton<Weblog.Core.Service.AI.Core.IAiKeyEncryptionService>(sp => 
    new Weblog.Core.Service.AI.Core.AesKeyEncryptionService(aesKey));
builder.Services.AddAiProviders();
builder.Services.AddSingleton<Weblog.Core.Service.AI.Core.AiProviderSelector>();
builder.Services.AddScoped<Weblog.Core.Service.AI.IAiProviderService, Weblog.Core.Service.AI.AiProviderService>();
builder.Services.AddSingleton<Weblog.Core.Service.AI.Plugins.PluginManager>();
builder.Services.AddScoped<Weblog.Core.Service.AI.IAiKernel, Weblog.Core.Service.AI.AiKernel>();
builder.Services.AddHttpClient<IGiphyService, GiphyService>();
builder.Services.AddHttpClient<ILinkPreviewService, LinkPreviewService>();

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "your_jwt_secret_key_here";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    System.Text.Json.JsonSerializer.Serialize(
                        new { code = 401, success = false, message = "未授权，请先登录" },
                        new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }
                    )
                );
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder
        .Tag("portal")
        .With(c => c.HttpContext.Request.Method == "POST" || c.HttpContext.Request.Method == "GET"));
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "Weblog Core API", 
        Version = "v1"
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseOutputCache();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var aiKernel = scope.ServiceProvider.GetRequiredService<Weblog.Core.Service.AI.IAiKernel>();
    await aiKernel.InitializeAsync();
}

app.Run();
