# Weblog.Core 更新日志

## 私密留言功能 (Spoiler/Masking Feature)

### 功能概述
新增私密留言功能，允许留言者发布只有知道密钥的人才能查看的私密内容。

### 后端实现

#### 1. 数据库结构变更
**表 `t_comment` 新增字段：**
```sql
ALTER TABLE t_comment ADD COLUMN IsSecret TINYINT(1) DEFAULT 0;
ALTER TABLE t_comment ADD COLUMN SecretContent TEXT;
ALTER TABLE t_comment ADD COLUMN SecretKeyHash VARCHAR(255);
```

**新建表 `comment_admin`：**
```sql
CREATE TABLE comment_admin (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    nickname VARCHAR(50),
    token VARCHAR(255),
    token_expire_time DATETIME,
    created_at DATETIME,
    updated_at DATETIME
);
```

#### 2. 实体类变更
**文件：** `src/01_Weblog.Core.Model/Entities/Comment.cs`
- 新增 `IsSecret` 字段 (bool)
- 新增 `SecretContent` 字段 (string, AES加密存储)
- 新增 `SecretKeyHash` 字段 (string, SHA256哈希存储)

#### 3. 新增实体类
**文件：** `src/01_Weblog.Core.Model/Entities/CommentAdmin.cs`
- 评论管理员实体，用于私密留言的发布认证

#### 4. 新增工具类
**文件：** `src/01_Weblog.Core.Model/Helpers/AesUtil.cs`
- `Encrypt(plainText, key)` - AES-256-CBC 加密
- `Decrypt(cipherText, key)` - AES-256-CBC 解密
- `HashSha256(input)` - SHA256 哈希
- `DeriveKey(userKey)` - 从任意长度密钥派生32位AES密钥

#### 5. 验证码防暴破
**文件：** `src/02_Weblog.Core.Common/Utils/CaptchaStore.cs`
- 内存存储验证码信息
- 支持失败次数限制（3次失败后需等待60秒）
- 5分钟过期机制

#### 6. 服务层变更
**文件：** `src/04_Weblog.Core.Service/Implements/CommentService.cs`
- `CreateAsync()` 新增私密内容加密存储
- `VerifySecretAsync()` 验证密钥并解密内容
- `GetCaptchaAsync()` 生成数学验证码

#### 7. API 接口
**文件：** `src/05_Weblog.Core.Api/Controllers/Portal/CommentAdminController.cs`
- `POST /api/comment-admin/login` - 管理员登录
- `POST /api/comment-admin/verify` - 验证登录状态
- `POST /api/comment-admin/logout` - 退出登录
- `GET /api/comment-admin/info` - 获取管理员信息

**文件：** `src/05_Weblog.Core.Api/Controllers/Portal/CommentController.cs`
- `GET /api/comment/captcha` - 获取数学验证码
- `POST /api/comment/verify-secret` - 验证私密内容密钥

### 前端实现

#### 1. 新增组件

**`CommentAdminLogin.vue`** - 管理员登录弹窗
- 用户名/密码登录表单
- 登录状态持久化到 Pinia store

**`SecretVerifyModal.vue`** - 私密内容验证弹窗
- 密钥输入框
- 数学验证码（加法题）
- 验证失败重试机制

**`SpoilerContent.vue`** - 私密内容显示组件
- 五彩马赛克遮罩效果（蓝、紫、粉、绿、黄）
- 渐变流动动画
- 像素化纹理叠加
- 闪光扫过效果
- 点击解锁交互
- 解锁时模糊+缩放动画

**`useMarkdown.js`** - Markdown 渲染 composable
- 使用 `marked` 库
- 使用 `DOMPurify` XSS 防护

#### 2. 组件变更

**`MessageWallForm.vue`**
- 新增私密内容开关
- 新增私密内容输入框
- 新增密钥输入框
- 管理员登录入口
- 私密内容加密提交

**`MessageWallCard.vue`**
- 集成 `SecretVerifyModal` 验证弹窗
- 私密内容显示 `SpoilerContent` 组件
- 解锁后渲染 Markdown 内容

**`NestedCommentItem.vue`**
- 同样支持私密内容显示
- 集成 `SecretVerifyModal` 验证弹窗

#### 3. API 和 Store

**`api/frontend/commentAdmin.js`**
- `commentAdminLogin()` - 管理员登录
- `commentAdminVerify()` - 验证登录状态
- `commentAdminLogout()` - 退出登录
- `commentAdminInfo()` - 获取管理员信息
- `getCaptcha()` - 获取验证码
- `verifySecret()` - 验证私密内容

**`stores/commentAdmin.js`** (Pinia Store)
- 管理员登录状态管理
- Token 持久化存储

### 安全机制

1. **AES-256-CBC 加密** - 私密内容加密存储
2. **SHA256 哈希** - 密钥不存储原值，仅存哈希
3. **密钥派生** - 任意长度密钥转换为32位AES密钥
4. **数学验证码** - 防止自动化攻击
5. **失败次数限制** - 3次失败后锁定60秒
6. **XSS 防护** - Markdown 内容经过 DOMPurify 消毒

### 用户流程

1. **发布私密留言：**
   - 管理员登录留言墙
   - 开启"私密内容"开关
   - 输入私密内容和密钥
   - 提交评论（内容加密存储）

2. **查看私密留言：**
   - 点击马赛克遮罩
   - 弹出验证弹窗
   - 输入正确密钥 + 数学验证码
   - 内容解密显示

### 配置说明

**管理员账号：**
- 用户名：`admin`
- 密码：`3126808313`
- 密码存储：SHA256 哈希

**初始管理员创建 SQL：**
```sql
SELECT SHA2('your_password', 256);  -- 生成密码哈希
INSERT INTO comment_admin (username, password_hash, nickname, created_at, updated_at)
VALUES ('admin', 'your_hash_here', 'Admin', NOW(), NOW());
```

### 文件清单

```
src/
├── 01_Weblog.Core.Model/
│   ├── Entities/Comment.cs          (修改)
│   ├── Entities/CommentAdmin.cs    (新增)
│   ├── DTOs/CommentAdminDto.cs    (新增)
│   └── Helpers/AesUtil.cs          (新增)
├── 02_Weblog.Core.Common/
│   └── Utils/CaptchaStore.cs       (新增)
├── 04_Weblog.Core.Service/
│   └── Implements/CommentService.cs (修改)
├── 05_Weblog.Core.Api/
│   ├── Controllers/Portal/CommentAdminController.cs (新增)
│   └── sql/002_add_spoiler_feature.sql (新增)

weblog-vue3/src/
├── api/frontend/commentAdmin.js    (新增)
├── stores/commentAdmin.js          (新增)
├── composables/useMarkdown.js      (新增)
├── components/
│   ├── CommentAdminLogin.vue      (新增)
│   ├── SecretVerifyModal.vue       (新增)
│   ├── SpoilerContent.vue          (新增)
│   ├── MessageWallForm.vue         (修改)
│   ├── MessageWallCard.vue         (修改)
│   └── NestedCommentItem.vue       (修改)
```

---

## 其他优化

### 代码优化
- `CaptchaStore` 从 Api 项目移至 Core.Common 项目，解决项目引用问题
- 修复 `isSecret` 布尔值类型问题（使用 `!!` 转换）
- 修复 `CommentAdmin` 实体列名映射（PascalCase to snake_case）

### 前端优化
- 马赛克效果升级为五彩渐变动画
- 新增像素化纹理叠加效果
- 新增闪光扫过动画
- 解锁动画增加模糊+缩放效果

---

## 私密内容过期与重置功能

### 功能概述
新增私密内容过期时间和"忘记密钥"重置功能。

### 后端实现

#### 1. 数据库结构变更
**表 `t_comment` 新增字段：**
```sql
ALTER TABLE t_comment ADD COLUMN ExpiresAt DATETIME NULL;
ALTER TABLE t_comment ADD COLUMN IsExpired TINYINT(1) DEFAULT 0;
ALTER TABLE t_comment ADD COLUMN IsReset TINYINT(1) DEFAULT 0;
```

#### 2. 实体类变更
**文件：** `src/01_Weblog.Core.Model/Entities/Comment.cs`
- 新增 `ExpiresAt` 字段 (DateTime?, 过期时间)
- 新增 `IsExpired` 字段 (bool, 是否已过期)
- 新增 `IsReset` 字段 (bool, 是否已重置)

**文件：** `src/01_Weblog.Core.Model/DTOs/CommentDto.cs`
- 新增 `ExpiresAt` 字段
- 新增 `IsExpired` 字段
- 新增 `IsReset` 字段

**文件：** `src/01_Weblog.Core.Model/DTOs/CommentAdminDto.cs`
- 新增 `ResetSecretRequest` 请求类

#### 3. 服务层变更
**文件：** `src/04_Weblog.Core.Service/Implements/CommentService.cs`
- `CreateAsync()` 新增 `ExpiresAt` 处理
- `VerifySecretAsync()` 新增过期和重置检查
- 新增 `ResetSecretAsync()` 方法 - 重置私密内容

#### 4. API 接口
**文件：** `src/05_Weblog.Core.Api/Controllers/Portal/CommentAdminController.cs`
- 新增 `POST /api/comment-admin/reset-secret` - 重置私密内容（需管理员认证）

### 前端实现

#### 1. 新增组件
**`ForgotKeyModal.vue`** - 忘记密钥重置弹窗
- 管理员身份验证
- 清除原私密内容
- 设置新私密内容和新密钥
- 设置过期时间

#### 2. 组件变更
**`SpoilerContent.vue`**
- 新增已过期状态显示（灰色时钟图标）
- 新增已重置状态显示（红色禁止图标）
- 新增"忘记密钥"按钮

**`MessageWallForm.vue`**
- 新增过期时间选择器
- 支持永不过期/1天/7天/30天/自定义时间
- 发布时传递过期时间

### 用户流程

1. **发布带过期时间的私密留言：**
   - 管理员登录留言墙
   - 开启私密内容
   - 输入内容和密钥
   - 选择过期时间
   - 发布

2. **查看已过期私密留言：**
   - 点击马赛克遮罩
   - 显示"已过期"状态（灰色）

3. **忘记密钥重置：**
   - 点击马赛克上的"忘记密钥"按钮
   - 登录管理员账号
   - 选择清除内容或设置新内容
   - 确认重置
