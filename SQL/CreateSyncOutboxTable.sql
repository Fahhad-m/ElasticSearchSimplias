-- =============================================================
-- Creates the SyncOutbox table for the Transactional Outbox pattern.
-- This table stores pending Elasticsearch sync operations.
-- A background service polls it and processes entries with retry.
-- =============================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SyncOutbox')
BEGIN
    CREATE TABLE SyncOutbox
    (
        Id              INT             IDENTITY(1,1) PRIMARY KEY,
        EntityId        INT             NOT NULL,
        EntityType      NVARCHAR(50)    NOT NULL DEFAULT 'Product',
        OperationType   NVARCHAR(20)    NOT NULL,   -- Index, Update, Delete
        Payload         NVARCHAR(MAX)   NULL,        -- JSON-serialized entity
        Status          NVARCHAR(20)    NOT NULL DEFAULT 'Pending',
        RetryCount      INT             NOT NULL DEFAULT 0,
        MaxRetries      INT             NOT NULL DEFAULT 3,
        CreatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
        LastAttemptAt   DATETIME2       NULL,
        NextRetryAt     DATETIME2       NULL,
        ErrorMessage    NVARCHAR(MAX)   NULL
    );

    -- Index for the background service polling query
    CREATE INDEX IX_SyncOutbox_Pending
        ON SyncOutbox (Status, NextRetryAt)
        WHERE Status = 'Pending';
END
GO