# 开发日志

## 2026-03-30

### 分类统计和标签统计接口修复

#### 问题描述
访问 `/api/admin/dashboard/category/statistics` 和 `/api/admin/dashboard/tag/statistics` 时请求卡住，无响应。

#### 根本原因
1. **数据库列名不匹配**：`ArticleTag` 和 `ArticleCategoryRel` 实体类没有指定 `[SugarColumn(ColumnName)]`，SqlSugar 自动将 PascalCase 属性名转为 snake_case（如 `TagId` → `tag_id`），但数据库中实际列名为 PascalCase（`TagId`、`CategoryId`），导致 `Unknown column 'tag_id'` / `Unknown column 'category_id'` 错误。

2. **全表加载到内存**：原代码将 `t_article_category_rel` 和 `t_article_tag` 全表数据拉到内存中做 GroupBy 分组，数据量大时性能差。

#### 解决方案

**修复1：添加列名映射**
```csharp
// ArticleTag.cs
[SugarColumn(ColumnName = "ArticleId")]
public long ArticleId { get; set; }

[SugarColumn(ColumnName = "TagId")]
public long TagId { get; set; }

// ArticleCategoryRel.cs
[SugarColumn(ColumnName = "ArticleId")]
public long ArticleId { get; set; }

[SugarColumn(ColumnName = "CategoryId")]
public long CategoryId { get; set; }

[SugarColumn(IsNullable = true, ColumnName = "CreateTime")]
public DateTime? CreateTime { get; set; }
```

**修复2：使用 SQL JOIN + GROUP BY 替代全表加载**
```csharp
// 优化前 - 2次全表查询 + 内存处理
var categories = await _dbContext.CategoryDb.Where(...).ToListAsync();
var relList = await _dbContext.ArticleCategoryRelDb.ToListAsync(); // 全表加载
var relCounts = relList.GroupBy(...); // 内存分组

// 优化后 - 1次 JOIN 查询，数据库直接出结果
var result = await _dbContext.Db.Queryable<ArticleCategoryRel>()
    .InnerJoin<Category>((rel, c) => rel.CategoryId == c.Id && !c.IsDeleted)
    .GroupBy((rel, c) => new { c.Id, c.Name })
    .Select((rel, c) => new CategoryStatisticsDto
    {
        Name = c.Name,
        Count = SqlFunc.AggregateCount(rel.Id)
    })
    .OrderByDescending(x => x.Count)
    .Take(10)
    .ToListAsync();
```

#### 修改文件
- `src/01_Weblog.Core.Model/Entities/ArticleTag.cs` - 添加 ColumnName 映射
- `src/01_Weblog.Core.Model/Entities/ArticleCategoryRel.cs` - 添加 ColumnName 映射
- `src/04_Weblog.Core.Service/Implements/DashboardService.cs` - JOIN 查询优化

---

### 前端请求路径双重 `/api/api/` 修复

#### 问题描述
所有 API 请求 URL 出现双重前缀 `/api/api/...`，导致 404。

#### 根本原因
`axios.js` 中 `baseURL` 被误设为 `"/api/api"`，与 API 调用路径叠加产生双重前缀。

#### 解决方案
```javascript
// 修复前
baseURL: "/api/api"

// 修复后
baseURL: "/api"
```

#### 修改文件
- `weblog-vue3/src/axios.js` - 修正 baseURL

---

### Nginx 代理路径修复

#### 问题描述
生产环境 Nginx 将 `/api/` 前缀剥离后转发给后端，导致后端路由匹配失败。

#### 根本原因
`proxy_pass http://weblogbackend:5000/` 末尾的 `/` 会将 `/api/xxx` 转发为 `/xxx`，但后端 Controller 路由为 `[Route("api/admin/...")]`，仍需 `/api` 前缀。

#### 解决方案
```nginx
# 修复前 - 去掉了 /api 前缀
proxy_pass http://weblogbackend:5000/;

# 修复后 - 保留 /api 前缀
proxy_pass http://weblogbackend:5000/api/;
```

#### 修改文件
- `weblog-vue3/nginx.conf` - 修正 proxy_pass 路径

---

## 2026-03-28

### 后台 Dashboard 加载慢问题修复

#### 问题描述
后台管理页面加载时反复请求 `/api/api/admin/dashboard/statistics`，导致加载缓慢。

#### 根本原因
`DashboardService.cs` 中的 `GetArticlePvTrendAsync` 方法存在 **N+1 查询问题**：

```csharp
// 修复前 - 循环中每天单独查询一次
for (int i = days - 1; i >= 0; i--)
{
    var date = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd");
    var stats = await _dbContext.StatisticsDb.FirstAsync(it => it.Date == date);
    // 7天 = 7次数据库查询
}
```

#### 解决方案
改为一次查询所有数据，然后在内存中处理：

```csharp
// 修复后 - 一次查询所有数据
var statsList = await _dbContext.StatisticsDb
    .Where(it => it.Date.CompareTo(startDateStr) >= 0)
    .Where(it => it.Date.CompareTo(endDateStr) <= 0)
    .ToListAsync();

var statsDict = statsList.ToDictionary(it => it.Date);

var result = new List<ArticlePvTrendDto>();
for (int i = days - 1; i >= 0; i--)
{
    var date = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd");
    var hasStats = statsDict.TryGetValue(date, out var stats);

    result.Add(new ArticlePvTrendDto
    {
        Date = date,
        ViewCount = hasStats ? stats.ViewCount : 0
    });
}
```

#### 修改文件
- `src/04_Weblog.Core.Service/Implements/DashboardService.cs`

#### 性能提升
- **修复前**：7 次数据库查询（N+1 问题）
- **修复后**：1 次数据库查询
- **提升**：减少 6 次数据库往返访问

#### 备注
- `Date` 字段在数据库中是 `string` 类型，因此使用 `string.CompareTo()` 替代 `>=` `<=` 操作符，以兼容 SqlSugar 表达式树的翻译
- 同样的优化也适用于 `GetCategoryStatisticsAsync` 和 `GetTagStatisticsAsync` 方法，它们之前也已经修复过

---

### 贴纸选择器重复加载问题修复

#### 问题描述
切换贴纸时每次都需要重新加载贴纸数据，导致加载慢。

#### 根本原因
`StickerPicker.vue` 组件使用 `watch` 监听 `show` 属性变化，每次打开都会调用 `loadPacks()` 重新加载：

```javascript
// 修复前 - 每次打开都加载
watch(() => props.show, (val) => {
    if (val) {
        loadPacks()
    }
})
```

#### 解决方案
添加缓存判断，只有在数据为空时才加载：

```javascript
// 修复后 - 只在第一次加载
watch(() => props.show, async (val) => {
    if (val && packs.value.length === 0) {
        await loadPacks()
    }
})
```

#### 修改文件
- `Blue岛/src/components/StickerPicker.vue`
- `weblog-vue3/src/components/StickerPicker.vue`

---

### MinIO 访问地址问题

#### 问题描述
数据库中存储的图片 URL 使用了 `http://127.0.0.1:9000/...` 地址，在 Docker 环境中无法访问。

#### 原因
1. 迁移了本地数据库，数据是旧的
2. Docker 容器间通信需要使用容器名称（如 `1Panel-minio-cRdZ:9000`），而不是 `127.0.0.1`
3. 1Panel 环境变量 `MinIO__PublicUrl` 覆盖了 appsettings.json 中的配置

#### 解决方案
1. 修改 1Panel 中的环境变量 `MinIO__PublicUrl` 为 `https://img.xuancangmenpro.online`
2. 或使用 SQL 脚本批量更新数据库中的旧 URL

---

### 前端后端 API 地址配置

#### 开发环境
使用 Vite 代理：
```javascript
// vite.config.js
server: {
    proxy: {
        '/api': {
            target: 'http://weblogbackend:5000',
            changeOrigin: true,
            rewrite: (path) => path.replace(/^\/api/, ''),
        },
    }
}
```

#### 生产环境（静态网站容器）
使用环境变量：
```javascript
// axios.js
const instance = axios.create({
    baseURL: import.meta.env.VITE_API_BASE_URL || "/api",
    timeout: 7000
})
```

```env
# .env.production
VITE_API_BASE_URL=https://你的后端域名.com/api
```

---

---

## 2026-04-03

### 前端 Docker 容器样式不更新问题修复

#### 问题描述
重新构建并推送 Docker 镜像后，部署的前端容器显示的仍然是旧样式，CSS/页面变更没有生效。

#### 根本原因（两个叠加问题）

**原因 1：Dockerfile 没有在容器内构建前端**

原始 Dockerfile 直接 `COPY ./dist`，依赖本地已经存在的构建产物：

```dockerfile
# 旧 Dockerfile（有问题）
FROM nginx:alpine
COPY ./dist /usr/share/nginx/html   # 直接拷贝本地 dist，不在容器内构建
COPY ./nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

这意味着如果本地 `dist/` 是旧的（或根本没运行过 `npm run build`），Docker 镜像里就是旧代码。

**原因 2：docker tag 指向了旧镜像，push 推送了错误版本**

执行流程存在 tag 不一致问题：

```bash
docker build -t weblog-web:latest .          # 构建新镜像，tag 为 weblog-web:latest
docker tag weblog-web-v2:latest xxx/...      # ❌ tag 的是旧镜像 weblog-web-v2，不是刚构建的
docker push xxx/weblog-web-v2:latest         # 推送了旧内容
```

正确的应该是：

```bash
docker build -t weblog-web:latest .
docker tag weblog-web:latest xxx/weblog-web:latest   # ✅ tag 刚构建的那个
docker push xxx/weblog-web:latest
```

#### 解决方案

**修复 1：改为多阶段构建，`npm run build` 在容器内自动执行**

```dockerfile
# ── 阶段一：构建前端 ──
FROM node:18-alpine AS builder
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm install --frozen-lockfile
COPY . .
RUN npm run build                              # 在容器内编译，不依赖本地 dist

# ── 阶段二：Nginx 托管 ──
FROM nginx:alpine
COPY --from=builder /app/dist /usr/share/nginx/html
COPY ./nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

**修复 2：build / tag / push 保持镜像名一致**

```bash
docker build -t weblog-web:latest .
docker tag weblog-web:latest 你的用户名/weblog-web:latest
docker push 你的用户名/weblog-web:latest
```

**修复 3：新增 `.dockerignore` 避免 node_modules 被打入构建上下文**

```
node_modules
dist
.git
.gitignore
*.md
```

#### 优点对比

| | 旧方式 | 新方式（多阶段） |
|---|---|---|
| 是否依赖本地 dist | 是，必须手动 `npm run build` | 否，容器内自动构建 |
| 样式是否保证最新 | 不保证（本地忘了 build 就是旧的） | 保证（每次 docker build 都重新编译） |
| 镜像大小 | 只含 nginx + dist，小 | 最终镜像同样只含 nginx + dist，小 |
| 构建上下文大小 | 如不忽略 node_modules 会很大 | `.dockerignore` 后只传源码，快 |

#### 修改文件
- `weblog-vue3/Dockerfile` - 改为多阶段构建
- `weblog-vue3/.dockerignore` - 新增，排除无关文件

---

### AI 插件市场无效插件移除

#### 问题描述
插件市场中展示了"翻译插件"、"标签推荐"、"编辑器助手"三个插件，但后端实际未实现对应功能，配置了也无法生效，给用户造成误导。

#### 解决方案
从 `ai-plugin.vue` 中移除这三个插件的相关逻辑：
1. 删除分类筛选中的 `editor`（编辑辅助）选项
2. 删除抽屉配置表单中三个插件专属的 `<template>` 配置块
3. 删除 `drawerForm` 中 `tagCount`、`defaultTargetLanguage` 字段
4. 删除 `openDrawer()` 和 `buildConfigJson()` 中对应的三个分支
5. 删除 `categoryMeta` 中的 `editor` 分类定义

#### 修改文件
- `weblog-vue3/src/pages/admin/ai-plugin.vue`

---

### 后台日期选择器输入框无边框修复

#### 问题描述
后台管理页面中所有日期选择器（单日期、日期区间）的输入框没有可见边框，视觉上无法识别为可交互的输入框。

#### 根本原因
Element Plus 的 `el-date-editor`（单日期）和 `el-range-editor`（日期区间）DOM 结构不同：

- **`el-date-editor`**：外层容器 + 内层 `.el-input__wrapper`，边框应加在**内层 `.el-input__wrapper`** 上
- **`el-range-editor`**：没有内层 wrapper，边框必须加在**外层容器自身** 上

原 CSS 将两者都设为 `border: none !important`，导致日期区间选择器完全没有边框。

```css
/* 旧代码（有问题）—— 两者都清除了边框 */
.el-date-editor {
  border: none !important;
}
.el-range-editor {
  border: none !important;  /* ❌ 区间选择器边框消失 */
}
```

#### 解决方案
拆分为两条独立规则，区间选择器保留边框：

```css
/* 单日期：外层清除，边框由内层 .el-input__wrapper 控制 */
.el-date-editor {
  border: none !important;
  box-shadow: none !important;
  background: transparent !important;
  width: auto !important;
  display: inline-flex !important;
}

/* 日期区间：边框直接加在外层容器上 */
.el-range-editor {
  border: 1px solid #d1d5db !important;
  border-radius: 6px !important;
  box-shadow: none !important;
  background: transparent !important;
  width: auto !important;
  display: inline-flex !important;
}
.el-range-editor:hover { border-color: #9ca3af !important; }
.el-range-editor.is-active { border-color: #374151 !important; box-shadow: none !important; }
```

暗色模式同样按此逻辑拆分。

#### 修改文件
- `weblog-vue3/src/assets/main.css` - 拆分 `el-date-editor` 和 `el-range-editor` 的 CSS 规则

---

## 历史记录

### 2026-03-XX (之前的修复)

#### StarryCanvas 组件
- 创建了 `StarryCanvas.vue` 组件，包含交互式星星和北斗七星星座
- 支持白天/夜晚模式切换

#### Modal 定位修复
- 使用 Vue `Teleport` 将模态框渲染到 `body`，避免 transform 上下文问题
- 修复了 `SecretVerifyModal`、`CommentAdminLogin`、`StickerPicker`、`GiphyPicker` 等组件

#### 仪表盘 N+1 查询修复
- `GetCategoryStatisticsAsync` - 使用单次 JOIN 查询替代循环查询
- `GetTagStatisticsAsync` - 使用单次 JOIN 查询替代循环查询

#### MinIO MD5 去重
- `MinIOService.cs` 实现基于 MD5 哈希的文件去重
- 相同内容的文件不会重复上传
