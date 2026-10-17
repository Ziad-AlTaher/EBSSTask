using System;
using System.Collections.Generic;
using System.Data;
using EBSSTask.Application.Common.Interfaces;
using EBSSTask.Application.Reports.Models;
using EBSSTask.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EBSSTask.Infrastructure.Persistence;

public partial class EliteDbContext : DbContext, IApplicationDbContext
{
    public EliteDbContext()
    {
    }

    public EliteDbContext(DbContextOptions<EliteDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Balance> Balances { get; set; }

    public virtual DbSet<BalanceHistory> BalanceHistories { get; set; }

    public virtual DbSet<FileProcess> FileProcesses { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Balance>(entity =>
        {
            entity.HasKey(e => e.BalanceId).HasName("PK_Balance_");

            entity.ToTable("Balance");

            entity.Property(e => e.BalanceId).HasColumnName("Balance_ID");
            entity.Property(e => e.AllowPointSystem)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasDefaultValueSql("((0))", "DF_Balance2_allow_point_system_")
                .HasColumnName("allow_point_system");
            entity.Property(e => e.BLevel)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasDefaultValueSql("((1))", "DF_Balance_B_Level_")
                .HasColumnName("B_Level");
            entity.Property(e => e.BalanceName).HasColumnName("Balance_Name");
            entity.Property(e => e.BalanceNameEn).HasColumnName("Balance_Name_en");
            entity.Property(e => e.BalanceType)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("Balance_Type");
            entity.Property(e => e.Fixed)
                .HasMaxLength(1)
                .IsFixedLength();
            entity.Property(e => e.OprKindD)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("opr_kind_d");
            entity.Property(e => e.OprMD)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("opr_m_d");
            entity.Property(e => e.OprType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("opr_type");
            entity.Property(e => e.OrgId)
                .HasDefaultValue(0, "DF_Balance2_org_id_")
                .HasColumnName("org_id");
            entity.Property(e => e.ParentCode).HasColumnName("Parent_Code");
            entity.Property(e => e.ParentId)
                .HasDefaultValue(0, "DF_Balance2_Parent_ID_")
                .HasColumnName("Parent_ID");
            entity.Property(e => e.Sub1Id)
                .HasDefaultValue(0, "DF_Balance2_Sub1_ID_")
                .HasColumnName("Sub1_ID");
            entity.Property(e => e.Sub2Id)
                .HasDefaultValue(0, "DF_Balance2_Sub2_ID_")
                .HasColumnName("Sub2_ID");
            entity.Property(e => e.Sub3Id)
                .HasDefaultValue(0, "DF_Balance2_Sub3_ID_")
                .HasColumnName("Sub3_ID");
            entity.Property(e => e.Sub4Id)
                .HasDefaultValue(0, "DF_Table_1_Sub1_ID1_")
                .HasColumnName("Sub4_ID");
            entity.Property(e => e.Sub5Id)
                .HasDefaultValue(0, "DF_Balance_Sub5_ID_")
                .HasColumnName("Sub5_ID");
            entity.Property(e => e.Sub6Id)
                .HasDefaultValue(0, "DF_Balance_Sub6_ID_")
                .HasColumnName("Sub6_ID");
            entity.Property(e => e.Sub7Id)
                .HasDefaultValue(0, "DF_Balance_Sub7_ID_")
                .HasColumnName("Sub7_ID");
        });

        modelBuilder.Entity<BalanceHistory>(entity =>
        {
            entity.HasKey(e => e.BalanceHisId);

            entity.ToTable("Balance_History");

            entity.Property(e => e.BalanceHisId).HasColumnName("Balance_His_ID");
            entity.Property(e => e.BalanceId).HasColumnName("Balance_ID");
            entity.Property(e => e.BillArrange).HasColumnName("bill_arrange");
            entity.Property(e => e.BranchId)
                .HasDefaultValue(0, "DF_Balance_History_branch_id")
                .HasColumnName("branch_id");
            entity.Property(e => e.CostCenterId)
                .HasDefaultValue(0, "DF_Balance_History_cost_center_id")
                .HasColumnName("cost_center_id");
            entity.Property(e => e.Creditor)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("creditor");
            entity.Property(e => e.Date)
                .HasDefaultValueSql("(getdate())", "DF_Balance_History_Date")
                .HasColumnType("datetime");
            entity.Property(e => e.Debtor)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("debtor");
            entity.Property(e => e.Este7kakDate)
                .HasColumnType("datetime")
                .HasColumnName("este7kak_date");
            entity.Property(e => e.InsertDate)
                .HasDefaultValueSql("(getdate())", "DF_Balance_History_insert_date")
                .HasColumnType("datetime")
                .HasColumnName("insert_date");
            entity.Property(e => e.Kind).HasMaxLength(2);
            entity.Property(e => e.NewBalnce)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("New_Balnce");
            entity.Property(e => e.OrderBill).HasColumnName("Order_Bill");
            entity.Property(e => e.OrgId).HasColumnName("org_id");
            entity.Property(e => e.PrevBalnce)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Prev_Balnce");
            entity.Property(e => e.ReferenceNo)
                .HasMaxLength(150)
                .HasColumnName("Reference_NO");
            entity.Property(e => e.RequestNo).HasColumnName("Request_NO");
        });

        modelBuilder.Entity<FileProcess>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("FileProcess");
            entity.Property(e => e.ProcessStatus).HasConversion<int>();
            entity.Property(e => e.ReportType).HasConversion<int>();
            entity.Property(e => e.ReportFormat).HasConversion<int>();
            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.ConnectionId).HasMaxLength(100);
            entity.Property(e => e.ErrorMessage).HasMaxLength(FileProcess.MaxErrorMessageLength);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    public async Task<ReportSummaryData> GetBalanceHistorySummaryAsync(
        int? balanceId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        var conn = Database.GetDbConnection();
        var wasClosed = conn.State != ConnectionState.Open;
        if (wasClosed)
        {
            await conn.OpenAsync(cancellationToken);
        }

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "dbo.sp_GetBalanceHistorySummary";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = 120;

            var pBalanceId = cmd.CreateParameter();
            pBalanceId.ParameterName = "@BalanceId";
            pBalanceId.Value = (object?)balanceId ?? DBNull.Value;
            cmd.Parameters.Add(pBalanceId);

            var pFromDate = cmd.CreateParameter();
            pFromDate.ParameterName = "@FromDate";
            pFromDate.Value = (object?)fromDate ?? DBNull.Value;
            cmd.Parameters.Add(pFromDate);

            var pToDate = cmd.CreateParameter();
            pToDate.ParameterName = "@ToDate";
            pToDate.Value = (object?)toDate ?? DBNull.Value;
            cmd.Parameters.Add(pToDate);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return new ReportSummaryData
                {
                    TotalRecords = reader.GetInt32(reader.GetOrdinal("TotalRecords")),
                    FirstPreviousBalance = reader.GetDecimal(reader.GetOrdinal("FirstPreviousBalance")),
                    TotalDebits = reader.GetDecimal(reader.GetOrdinal("TotalDebits")),
                    TotalCredits = reader.GetDecimal(reader.GetOrdinal("TotalCredits")),
                    FinalBalance = reader.GetDecimal(reader.GetOrdinal("FinalBalance")),
                    TotalPreviousBalances = reader.GetDecimal(reader.GetOrdinal("TotalPreviousBalances")),
                    TotalFinalBalances = reader.GetDecimal(reader.GetOrdinal("TotalFinalBalances")),
                    AccountType = reader.IsDBNull(reader.GetOrdinal("AccountType")) ? "D" : reader.GetString(reader.GetOrdinal("AccountType"))
                };
            }

            return new ReportSummaryData();
        }
        finally
        {
            if (wasClosed)
            {
                await conn.CloseAsync();
            }
        }
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
