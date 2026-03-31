-- Add IsAdmin and CommentAdminId columns to t_comment
ALTER TABLE t_comment ADD COLUMN IsAdmin TINYINT(1) DEFAULT 0;
ALTER TABLE t_comment ADD COLUMN CommentAdminId BIGINT NULL;
