-- 添加邮件通知功能相关字段
ALTER TABLE `t_blog_settings` 
ADD COLUMN `IsEmailNotificationOpen` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '是否开启邮件通知' AFTER `LinkPreviewWhitelist`,
ADD COLUMN `SmtpHost` VARCHAR(100) NULL COMMENT 'SMTP 主机地址' AFTER `IsEmailNotificationOpen`,
ADD COLUMN `SmtpPort` INT NOT NULL DEFAULT 587 COMMENT 'SMTP 端口' AFTER `SmtpHost`,
ADD COLUMN `SmtpUsername` VARCHAR(100) NULL COMMENT 'SMTP 用户名' AFTER `SmtpPort`,
ADD COLUMN `SmtpPassword` VARCHAR(100) NULL COMMENT 'SMTP 密码' AFTER `SmtpUsername`,
ADD COLUMN `SmtpEnableSsl` TINYINT(1) NOT NULL DEFAULT 1 COMMENT '是否启用 SSL' AFTER `SmtpPassword`,
ADD COLUMN `SmtpFromEmail` VARCHAR(100) NULL COMMENT '发件人邮箱' AFTER `SmtpEnableSsl`,
ADD COLUMN `SmtpFromName` VARCHAR(50) NULL COMMENT '发件人名称' AFTER `SmtpFromEmail`;
