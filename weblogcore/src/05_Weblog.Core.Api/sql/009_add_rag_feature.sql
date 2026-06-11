-- RAG 知识库功能迁移脚本
-- 执行时间：2025
-- 说明：SqlSugar CodeFirst 会自动建表，此脚本仅供手动维护参考

CREATE TABLE IF NOT EXISTS `t_kb` (
    `Id`                BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `Name`              VARCHAR(100) NOT NULL COMMENT '知识库名称',
    `Description`       VARCHAR(500) NULL COMMENT '描述',
    `EmbeddingProvider` VARCHAR(50)  NOT NULL DEFAULT 'openai' COMMENT 'Embedding Provider 名称',
    `EmbeddingModel`    VARCHAR(100) NOT NULL DEFAULT 'text-embedding-3-small' COMMENT 'Embedding 模型',
    `ChunkSize`         INT NOT NULL DEFAULT 500 COMMENT '切片大小（字符数）',
    `ChunkOverlap`      INT NOT NULL DEFAULT 50  COMMENT '切片重叠字符数',
    `IsEnabled`         TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedAt`         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt`         DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='知识库';

CREATE TABLE IF NOT EXISTS `t_kb_document` (
    `Id`           BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `KbId`         BIGINT NOT NULL COMMENT '所属知识库 ID',
    `Title`        VARCHAR(200) NOT NULL COMMENT '文档标题',
    `SourceType`   VARCHAR(20)  NOT NULL DEFAULT 'upload' COMMENT '来源类型：article/wiki/upload',
    `SourceId`     BIGINT NOT NULL DEFAULT 0 COMMENT '来源 ID',
    `Content`      LONGTEXT NULL COMMENT '原始文本内容',
    `Status`       VARCHAR(20) NOT NULL DEFAULT 'pending' COMMENT '状态：pending/indexing/indexed/failed',
    `ErrorMessage` VARCHAR(500) NULL COMMENT '失败原因',
    `ChunkCount`   INT NOT NULL DEFAULT 0 COMMENT '切片数量',
    `CreatedAt`    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt`    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX `idx_kb_id` (`KbId`),
    INDEX `idx_status` (`Status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='知识库文档';

CREATE TABLE IF NOT EXISTS `t_kb_chunk` (
    `Id`           BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `KbId`         BIGINT NOT NULL COMMENT '所属知识库 ID',
    `DocumentId`   BIGINT NOT NULL COMMENT '所属文档 ID',
    `Content`      LONGTEXT NOT NULL COMMENT '切片文本',
    `ChunkIndex`   INT NOT NULL DEFAULT 0 COMMENT '切片序号',
    `Vector`       LONGTEXT NULL COMMENT '向量 JSON（float[]）',
    `TokenCount`   INT NOT NULL DEFAULT 0 COMMENT '估算 token 数',
    `CreatedAt`    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX `idx_kb_id`  (`KbId`),
    INDEX `idx_doc_id` (`DocumentId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='文档切片（向量块）';
