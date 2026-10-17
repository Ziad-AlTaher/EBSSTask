-- =========================================================================
-- Stored Procedure: dbo.sp_GetBalanceHistorySummary
-- Description: Computes summary totals (First Previous Balance, Total Debits,
--              Total Credits, and Final Balance based on account type D/C)
--              for the Balance History report without text search.
-- =========================================================================

CREATE OR ALTER PROCEDURE dbo.sp_GetBalanceHistorySummary
    @BalanceId INT = NULL,
    @FromDate DATETIME2 = NULL,
    @ToDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FirstPreviousBalance DECIMAL(18, 2) = 0;
    DECLARE @TotalDebits DECIMAL(18, 2) = 0;
    DECLARE @TotalCredits DECIMAL(18, 2) = 0;
    DECLARE @FinalBalance DECIMAL(18, 2) = 0;
    DECLARE @TotalPreviousBalances DECIMAL(18, 2) = 0;
    DECLARE @TotalFinalBalances DECIMAL(18, 2) = 0;
    DECLARE @TotalCount INT = 0;
    DECLARE @AccountType NVARCHAR(10) = 'D';

    -- First previous balance from the first matching row (ordered by Balance_His_ID ASC)
    SELECT TOP 1 
        @FirstPreviousBalance = ISNULL(h.Prev_Balnce, 0),
        @AccountType = ISNULL(b.Balance_Type, 'D')
    FROM Balance_History h
    LEFT JOIN Balance b ON h.Balance_ID = b.Balance_ID
    WHERE (@BalanceId IS NULL OR h.Balance_ID = @BalanceId)
      AND (@FromDate IS NULL OR h.Date >= @FromDate)
      AND (@ToDate IS NULL OR h.Date <= @ToDate)
    ORDER BY h.Balance_His_ID ASC;

    -- Aggregate totals for table footer and account summary
    SELECT 
        @TotalCount = COUNT(1),
        @TotalDebits = ISNULL(SUM(h.debtor), 0),
        @TotalCredits = ISNULL(SUM(h.creditor), 0),
        @TotalPreviousBalances = ISNULL(SUM(h.Prev_Balnce), 0),
        @TotalFinalBalances = ISNULL(SUM(
            CASE 
                WHEN ISNULL(b.Balance_Type, 'D') = 'C' 
                    THEN (ISNULL(h.Prev_Balnce, 0) + ISNULL(h.creditor, 0) - ISNULL(h.debtor, 0))
                ELSE 
                    (ISNULL(h.Prev_Balnce, 0) + ISNULL(h.debtor, 0) - ISNULL(h.creditor, 0))
            END
        ), 0)
    FROM Balance_History h
    LEFT JOIN Balance b ON h.Balance_ID = b.Balance_ID
    WHERE (@BalanceId IS NULL OR h.Balance_ID = @BalanceId)
      AND (@FromDate IS NULL OR h.Date >= @FromDate)
      AND (@ToDate IS NULL OR h.Date <= @ToDate);

    -- Final Balance calculation:
    -- If account type = Debit (D) -> ((Previous Balance + Debit) - Credit)
    -- If account type = Credit (C) -> ((Previous Balance + Credit) - Debit)
    IF @AccountType = 'C'
        SET @FinalBalance = (@FirstPreviousBalance + @TotalCredits) - @TotalDebits;
    ELSE
        SET @FinalBalance = (@FirstPreviousBalance + @TotalDebits) - @TotalCredits;

    SELECT 
        @TotalCount AS TotalRecords,
        @FirstPreviousBalance AS FirstPreviousBalance,
        @TotalDebits AS TotalDebits,
        @TotalCredits AS TotalCredits,
        @FinalBalance AS FinalBalance,
        @TotalPreviousBalances AS TotalPreviousBalances,
        @TotalFinalBalances AS TotalFinalBalances,
        @AccountType AS AccountType;
END
