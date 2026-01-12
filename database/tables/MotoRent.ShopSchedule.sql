-- ShopSchedule table
-- Stores operating hours for specific dates. Rolling 8-week schedule auto-populated from previous week.
CREATE TABLE [<schema>].[ShopSchedule]
(
    [ShopScheduleId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for querying
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [Date] AS CAST(JSON_VALUE([Json], '$.Date') AS DATE),
    [IsOpen] AS CAST(COALESCE(JSON_VALUE([Json], '$.IsOpen'), 'true') AS BIT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

-- Unique constraint: one schedule entry per shop per date
CREATE UNIQUE INDEX IX_ShopSchedule_ShopDate ON [<schema>].[ShopSchedule]([ShopId], [Date])
GO

-- Index for date range queries (future schedules)
CREATE INDEX IX_ShopSchedule_DateRange ON [<schema>].[ShopSchedule]([ShopId], [Date])
    WHERE [Date] >= CAST(GETDATE() AS DATE)
GO
