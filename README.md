# Weblog.Core 博客系统

<div align="center">

![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-8.0-512BD4?logo=dotnet)
![Vue 3](https://img.shields.io/badge/Vue-3.x-4FC08D?logo=vue.js)
![MySQL](https://img.shields.io/badge/MySQL-8.0-4479A1?logo=mysql)
![License](https://img.shields.io/badge/License-MIT-green)

**一个功能完善的全栈博客系统，集成多模型 AI 写作助手、RAG 知识库和智能聊天。**

</div>

---

## ✨ 项目简介

Weblog.Core 是一个基于 **ASP.NET Core 8.0 + Vue 3** 的前后端分离个人博客系统，采用 DDD（领域驱动设计）架构风格。

**v2 版本** 在 v1 基础上大幅增强了 AI 功能，包括：
- 🤖 **AI 写作助手**：一键生成文章、SEO 优化、内容安全检测
- 📚 **RAG 知识库**：向量检索+关键词混合搜索，让 AI 基于你的私有内容回答
- 💬 **智能聊天**：支持 8+ 大模型，SSE 流式输出，会话管理
- 🔌 **插件市场**：可插拔 AI 插件架构，支持标签推荐、翻译、文章摘要等

---

## 🚀 功能特性

### 博客核心
- 📝 文章管理（Markdown 编辑器、分类、标签、封面图）
- 📁 Wiki 知识库（分目录组织，类似 Notion）
- 💬 评论系统（支持匿名评论、敏感词过滤）
- 🔍 全文搜索
- 📊 后台数据统计 Dashboard

### AI 智能模块
- 🤖 **多模型支持**：OpenAI / DeepSeek / Claude / Gemini / 智谱 GLM / 百度千帆 / MiniMax
- 📝 **AI 写作助手**：文章生成（可指定标题、大纲、风格、字数）、SEO 优化、内容审核
- 📚 **RAG 知识库**：上传文档、导入文章/Wiki，支持向量+关键词混合检索
- 💬 **AI 聊天**：前台流式聊天，可接入知识库进行基于上下文的问答
- ✍️ **编辑器助手**：写作过程中实时 AI 辅助
- 🔑 **多 Key 轮转**：同一 Provider 支持多个 API Key，自动故障转移

### 系统特性
- 🔐 JWT 认证鉴权
- 🗄️ MinIO 对象存储（图片/文件上传）
- 🐳 Docker 容器化部署
- ⚡ 输出缓存（OutputCache）

---

## 🛠️ 技术栈

| 类别 | 技术 |
|------|------|
| 后端框架 | ASP.NET Core 8.0 |
| 前端框架 | Vue 3 + Element Plus + TailwindCSS |
| 数据库 | MySQL 8.0 |
| ORM | SqlSugar |
| 认证 | JWT Bearer |
| AI 集成 | OpenAI / DeepSeek / Claude / Gemini / GLM / 千帆 |
| 向量检索 | 自实现余弦相似度（纯代码，无需 pgvector） |
| 文件存储 | MinIO |
| 容器化 | Docker + Docker Compose |

---

## 📁 项目结构

```
Weblog.Core/
├── src/                              # 后端（C# / ASP.NET Core）
│   ├── 01_Weblog.Core.Model          # 实体层 - 数据模型、DTO
│   ├── 02_Weblog.Core.Common         # 公共层 - JWT、Result封装
│   ├── 03_Weblog.Core.Repository     # 仓储层 - DbContext
│   ├── 04_Weblog.Core.Service        # 服务层 - 业务逻辑
│   │   └── AI/
│   │       ├── Providers/            # AI Provider 实现
│   │       ├── Plugins/              # AI 插件（文章摘要/写作/翻译等）
│   │       ├── Rag/                  # RAG 知识库服务
│   │       └── Core/                 # Provider 选择器、Key 加密
│   └── 05_Weblog.Core.Api            # 接口层 - Controller
│       └── Controllers/
│           ├── Admin/                # 后台接口
│           └── Portal/               # 前台接口
│
├── weblog-vue3/                      # 前端（Vue 3）
│   └── src/
│       ├── pages/
│       │   ├── admin/               # 后台页面
│       │   │   ├── ai-assistant.vue # AI 写作助手
│       │   │   ├── knowledge-base.vue # RAG 知识库管理
│       │   │   ├── ai-provider.vue  # AI Provider 配置
│       │   │   └── ai-plugin.vue    # AI 插件市场
│       │   └── frontend/            # 前台页面
│       │       └── chat.vue         # AI 聊天页
│       ├── components/
│       │   ├── chat/                # 聊天组件
│       │   └── AiSummaryCard.vue    # 文章AI摘要卡片
│       └── api/admin/               # API 封装
│
└── docs/                            # 项目文档
    ├── backend-technical-docs.md    # 后端技术文档
    ├── RAG知识库使用指南.md
    └── 教程/                        # 开发教程
```

---

## ⚡ 快速开始

### 前置要求
- .NET 8.0 SDK
- Node.js 18+
- MySQL 8.0
- MinIO（或阿里云OSS等兼容S3协议的存储）

### 1. 克隆仓库

```bash
git clone git@github.com:1093148685/weblogcode.git
cd weblogcode
git checkout v2
```

### 2. 配置后端

编辑 `src/05_Weblog.Core.Api/appsettings.json`：

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=weblog;User=root;Password=your_password;"
  },
  "Jwt": {
    "SecretKey": "your-secret-key-at-least-32-chars",
    "Issuer": "weblog",
    "Audience": "weblog"
  },
  "Minio": {
    "Endpoint": "localhost:9000",
    "AccessKey": "your-access-key",
    "SecretKey": "your-secret-key",
    "BucketName": "weblog"
  }
}
```

### 3. 启动后端

```bash
cd src/05_Weblog.Core.Api
dotnet run
# API 将运行在 http://localhost:5127
```

首次启动会自动通过 CodeFirst 建表。

### 4. 配置前端

```bash
cd weblog-vue3
npm install
# 编辑 vite.config.js 中的 proxy target 指向后端地址
npm run dev
```

### 5. Docker 部署

```bash
# 在项目根目录
docker-compose up -d
```

---

## 🤖 AI 功能配置

### 配置 AI Provider

1. 登录后台（默认 `/admin`）
2. 进入 **AI 管理 → AI Provider**
3. 添加 Provider（选择类型：OpenAI / DeepSeek / Claude 等）
4. 填入 API Key，系统自动加密存储

### 使用 AI 写作助手

后台 → **AI 管理 → AI 写作助手**

- **文章生成**：输入标题 + 可选大纲，选择写作风格和字数，一键生成
- **SEO 优化**：输入文章内容，AI 生成 SEO 建议
- **内容审核**：检测文章内容是否合规

### 配置 RAG 知识库

后台 → **AI 管理 → RAG 知识库**

1. 新建知识库，选择 Embedding Provider 和模型
2. 导入文章 / Wiki / 上传文档（支持 `.txt` `.md` `.pdf`）
3. 等待索引完成（状态变为 `indexed`）
4. 前台聊天页选择知识库，AI 将基于你的内容回答

> **注意**：RAG 知识库使用纯代码实现的余弦相似度，**不需要** pgvector 等向量数据库扩展。

---

## 📖 API 文档

后端启动后，访问 Swagger 文档：

```
http://localhost:5127/swagger
```

### 主要接口

| 模块 | 路径前缀 | 说明 |
|------|---------|------|
| 前台文章 | `/api/article` | 文章列表、详情、归档 |
| 前台聊天 | `/api/ai/chat` | SSE 流式 AI 聊天 |
| 后台文章 | `/api/admin/article` | 文章 CRUD |
| AI Provider | `/api/admin/ai/provider` | Provider 管理 |
| AI 写作助手 | `/api/admin/ai/assistant` | 文章生成、SEO、内容审核 |
| RAG 知识库 | `/api/admin/rag` | 知识库 CRUD、文档导入、检索测试 |

---

## 🔧 AI 写作助手 API（v2 新增）

### 生成文章

```http
POST /api/admin/ai/assistant/generate-article
Authorization: Bearer <token>

{
  "title": "Vue3 响应式原理深度解析",
  "outline": "1. 什么是响应式 2. Proxy 原理 3. 实战应用",
  "style": "技术",    // 技术 | 随笔 | 分析 | 新闻
  "wordCount": 1500
}
```

### SEO 优化

```http
POST /api/admin/ai/assistant/seo-optimize
Authorization: Bearer <token>

{
  "title": "文章标题",
  "content": "文章内容...",
  "keywords": "关键词1,关键词2"
}
```

### 内容审核

```http
POST /api/admin/ai/assistant/moderate
Authorization: Bearer <token>

{
  "content": "待审核的内容..."
}
```

---

## 📚 文档

| 文档 | 链接 |
|------|------|
| 后端技术文档 | [docs/backend-technical-docs.md](docs/backend-technical-docs.md) |
| RAG 知识库使用指南 | [docs/RAG知识库使用指南.md](docs/RAG知识库使用指南.md) |
| AI 功能开发教程 | [docs/教程/AI功能开发教程.md](docs/教程/AI功能开发教程.md) |
| 项目部署教程 | [docs/教程/项目部署教程.md](docs/教程/项目部署教程.md) |

---

## 🗓️ 版本记录

### v2（当前）
- ✅ AI 写作助手（文章生成 / SEO / 审核）
- ✅ RAG 知识库（向量+关键词混合检索）
- ✅ 前台 AI 聊天集成知识库
- ✅ AI 插件市场（可插拔架构）
- ✅ 多 AI Provider 支持（8+ 模型）
- ✅ Token 用量统计

### v1
- ✅ 博客核心功能（文章/分类/标签/评论）
- ✅ Wiki 知识库
- ✅ 后台管理
- ✅ 文章 AI 摘要

---

## 🤝 贡献

欢迎提 Issue 和 PR！

---

## 📄 License

[MIT](LICENSE)
