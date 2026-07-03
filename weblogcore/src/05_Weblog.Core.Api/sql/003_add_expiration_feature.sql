-- Spoiler Feature Enhancement Migration
-- Run this script to add expiration and reset functionality

-- Add expiration and reset columns to t_comment table
ALTER TABLE t_comment ADD COLUMN ExpiresAt DATETIME NULL;
ALTER TABLE t_comment ADD COLUMN IsExpired TINYINT(1) DEFAULT 0;
ALTER TABLE t_comment ADD COLUMN IsReset TINYINT(1) DEFAULT 0;
