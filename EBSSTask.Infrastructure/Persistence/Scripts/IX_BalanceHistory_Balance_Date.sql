-- =========================================================================
-- Index: IX_BalanceHistory_Balance_Date
-- Table: dbo.Balance_History
-- Description: Composite Non-Clustered Index on (Balance_ID, Date, Balance_His_ID)
--              including financial columns (debtor, creditor, Prev_Balnce).
--              Optimizes account ledger filtering, date range lookups, sorting,
--              and summary calculations without key lookups.
-- =========================================================================

IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = N'IX_BalanceHistory_Balance_Date' 
      AND object_id = OBJECT_ID(N'dbo.Balance_History')
)
BEGIN
    PRINT 'Creating Non-Clustered Index IX_BalanceHistory_Balance_Date on dbo.Balance_History...';

    CREATE NONCLUSTERED INDEX [IX_BalanceHistory_Balance_Date] ON [dbo].[Balance_History]
    (
        [Balance_ID] ASC,
        [Date] ASC,
        [Balance_His_ID] ASC
    )
    INCLUDE([debtor],[creditor],[Prev_Balnce]) 
    WITH (
        PAD_INDEX = OFF, 
        STATISTICS_NORECOMPUTE = OFF, 
        SORT_IN_TEMPDB = OFF, 
        DROP_EXISTING = OFF, 
        ONLINE = OFF, 
        ALLOW_ROW_LOCKS = ON, 
        ALLOW_PAGE_LOCKS = ON, 
        OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF
    ) ON [PRIMARY];

    PRINT 'Index IX_BalanceHistory_Balance_Date created successfully.';
END
ELSE
BEGIN
    PRINT 'Index IX_BalanceHistory_Balance_Date already exists.';
END
GO
