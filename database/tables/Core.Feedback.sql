CREATE TABLE [Core].[Feedback]
(
    [FeedbackId]       INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [AccountNo]        AS CAST(JSON_VALUE([Json], '$.AccountNo') AS VARCHAR(50)),
    [UserName]         AS CAST(JSON_VALUE([Json], '$.UserName') AS VARCHAR(200)),
    [Type]             AS CAST(JSON_VALUE([Json], '$.Type') AS VARCHAR(20)),
    [Status]           AS CAST(JSON_VALUE([Json], '$.Status') AS VARCHAR(20)),
    [Url]              AS CAST(JSON_VALUE([Json], '$.Url') AS NVARCHAR(500)),
    [LogEntryId]       AS CAST(JSON_VALUE([Json], '$.LogEntryId') AS INT),
    [Timestamp]        DATETIMEOFFSET NULL,
    [Json]             NVARCHAR(MAX) NOT NULL,
    [CreatedBy]        VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy]        VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IDX_Core_Feedback_AccountStatus
    ON [Core].[Feedback]([AccountNo], [Status])

CREATE INDEX IDX_Core_Feedback_StatusTimestamp
    ON [Core].[Feedback]([Status], [Timestamp])

CREATE INDEX IDX_Core_Feedback_LogEntryId
    ON [Core].[Feedback]([LogEntryId])
