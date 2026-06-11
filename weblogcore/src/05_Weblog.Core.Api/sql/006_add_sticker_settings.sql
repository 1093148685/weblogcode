-- 博客设置表添加贴纸包ZIP解压最大张数字段
ALTER TABLE `t_blog_settings` ADD COLUMN `StickerZipMaxCount` INT NOT NULL DEFAULT 100 COMMENT '贴纸包ZIP解压最大张数' AFTER `IsCommentExamineOpen`;