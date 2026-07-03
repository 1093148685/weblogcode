-- 添加链接预览功能相关字段
ALTER TABLE `t_blog_settings`
ADD COLUMN `IsLinkPreviewOpen` TINYINT(1) NOT NULL DEFAULT 1 COMMENT '是否开启评论链接预览' AFTER `StickerZipMaxCount`;

ALTER TABLE `t_blog_settings`
ADD COLUMN `LinkPreviewWhitelist` VARCHAR(2000) DEFAULT '' COMMENT '链接预览域名白名单（换行分隔，支持泛化）' AFTER `IsLinkPreviewOpen`;

-- 链接预览缓存表
-- 注意：Url 不能作为 VARCHAR(2000) 主键，否则 utf8mb4 下会超过 MySQL 3072 bytes 索引长度限制。
CREATE TABLE `t_link_preview_cache` (
  `Id` BIGINT NOT NULL AUTO_INCREMENT COMMENT '主键ID',
  `Url` VARCHAR(2000) NOT NULL COMMENT '链接URL',
  `Title` VARCHAR(500) NOT NULL COMMENT '页面标题',
  `Description` VARCHAR(1000) COMMENT '页面描述',
  `ImageUrl` VARCHAR(500) COMMENT '预览图片URL',
  `FaviconUrl` VARCHAR(500) COMMENT '网站图标URL',
  `Domain` VARCHAR(200) NOT NULL COMMENT '域名',
  `CreateTime` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `UpdateTime` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 表情包/贴纸内容哈希，用于去重
ALTER TABLE `t_sticker`
ADD COLUMN `ContentHash` VARCHAR(64) COMMENT '文件内容哈希' AFTER `IsAnimated`;
