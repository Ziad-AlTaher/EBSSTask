-- =========================================================================
-- Table: dbo.FileProcess
-- Description: Stores asynchronous background export requests and file metadata
--              for PDF and Excel reports with SignalR notification tracking.
-- =========================================================================

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'[dbo].[FileProcess]', N'U') IS NULL
BEGIN
    PRINT 'Creating Table [dbo].[FileProcess]...';

    CREATE TABLE [dbo].[FileProcess](
        [Id] [uniqueidentifier] NOT NULL,
        [ProcessStatus] [int] NOT NULL,
        [ReportType] [int] NOT NULL,
        [ReportFormat] [int] NOT NULL,
        [Parameters] [nvarchar](max) NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        [StartedAt] [datetime2](7) NULL,
        [CompletedAt] [datetime2](7) NULL,
        [FilePath] [nvarchar](500) NULL,
        [UserId] [nvarchar](100) NULL,
        [ConnectionId] [nvarchar](100) NULL,
        [ErrorMessage] [nvarchar](4000) NULL,
        CONSTRAINT [PK_FileProcess] PRIMARY KEY CLUSTERED 
        (
            [Id] ASC
        ) WITH (
            PAD_INDEX = OFF,
            STATISTICS_NORECOMPUTE = OFF,
            IGNORE_DUP_KEY = OFF,
            ALLOW_ROW_LOCKS = ON,
            ALLOW_PAGE_LOCKS = ON,
            OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF
        ) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];

    PRINT 'Table [dbo].[FileProcess] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [dbo].[FileProcess] already exists.';
END
GO
