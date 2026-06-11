-- Spoiler Feature Migration
-- Run this script to add support for secret/spoiler comments

-- Add columns to t_comment table for secret content
ALTER TABLE t_comment ADD COLUMN IsSecret TINYINT(1) DEFAULT 0;
ALTER TABLE t_comment ADD COLUMN SecretContent TEXT;
ALTER TABLE t_comment ADD COLUMN SecretKeyHash VARCHAR(255);

-- Create comment_admin table for admin login
CREATE TABLE IF NOT EXISTS comment_admin (
    id BIGINT PRIMARY KEY AUTO_INCREMENT,
    username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    nickname VARCHAR(50),
    token VARCHAR(255),
    token_expire_time DATETIME,
    created_at DATETIME,
    updated_at DATETIME
);

-- =====================================================
-- IMPORTANT: Create your initial admin account manually
-- =====================================================
-- The password must be SHA256 hashed before storage.
-- 
-- Example to generate hash in MySQL:
-- SELECT SHA2('your_password_here', 256) AS password_hash;
--
-- Then insert:
-- INSERT INTO comment_admin (username, password_hash, nickname, created_at, updated_at)
-- VALUES ('admin', 'your_hash_here', 'Admin', NOW(), NOW());
