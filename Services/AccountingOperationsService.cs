using AIReception.Mvc.Data;
using AIReception.Mvc.Data.Entities;
using AIReception.Mvc.Models.Dms;
using Microsoft.EntityFrameworkCore;

namespace AIReception.Mvc.Services;

public class AccountingOperationsService
{
    private readonly IDbContextFactory<IngridDmsDbContext> _dbContextFactory;
    private readonly DmsCoreService _dmsCore;

    public AccountingOperationsService(IDbContextFactory<IngridDmsDbContext> dbContextFactory, DmsCoreService dmsCore)
    {
        _dbContextFactory = dbContextFactory;
        _dmsCore = dmsCore;
    }

    public IReadOnlyList<GlAccountRecord> GetGlAccounts()
    {
        using var db = _dbContextFactory.CreateDbContext();
        return db.GlAccounts.OrderBy(x => x.AccountNumber).AsEnumerable().Select(MapGlAccount).ToList();
    }

    public IReadOnlyList<GlEntryRecord> GetGlEntries(Guid? repairOrderId = null)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var query = db.GlEntries.AsQueryable();
        if (repairOrderId.HasValue)
        {
            query = query.Where(x => x.RepairOrderId == repairOrderId.Value);
        }

        return query.OrderByDescending(x => x.PostedAtUtc).AsEnumerable().Select(MapGlEntry).ToList();
    }

    public IReadOnlyList<AccountsPayableBillRecord> GetAccountsPayableBills()
    {
        using var db = _dbContextFactory.CreateDbContext();
        return db.AccountsPayableBills.OrderByDescending(x => x.CreatedAtUtc).AsEnumerable().Select(MapApBill).ToList();
    }

    public IReadOnlyList<AccountsReceivableInvoiceRecord> GetAccountsReceivableInvoices(Guid? repairOrderId = null)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var query = db.AccountsReceivableInvoices.AsQueryable();
        if (repairOrderId.HasValue)
        {
            query = query.Where(x => x.RepairOrderId == repairOrderId.Value);
        }

        return query.OrderByDescending(x => x.CreatedAtUtc).AsEnumerable().Select(MapArInvoice).ToList();
    }

    public IReadOnlyList<BankReconciliationRecord> GetBankReconciliations()
    {
        using var db = _dbContextFactory.CreateDbContext();
        return db.BankReconciliations.OrderByDescending(x => x.StatementEndingAtUtc).AsEnumerable().Select(MapBankRec).ToList();
    }

    public IReadOnlyList<AccountingClosePeriodRecord> GetClosePeriods()
    {
        using var db = _dbContextFactory.CreateDbContext();
        return db.AccountingClosePeriods.OrderByDescending(x => x.PeriodEndUtc).AsEnumerable().Select(MapClosePeriod).ToList();
    }

    public GlAccountRecord CreateGlAccount(CreateGlAccountRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var entity = new DmsGlAccountEntity
        {
            Id = Guid.NewGuid(),
            AccountNumber = string.IsNullOrWhiteSpace(request.AccountNumber) ? $"GL-{DateTime.UtcNow:HHmmss}" : request.AccountNumber.Trim().ToUpperInvariant(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? "General ledger account" : request.Description.Trim(),
            AccountType = string.IsNullOrWhiteSpace(request.AccountType) ? "asset" : request.AccountType.Trim().ToLowerInvariant(),
            Department = (request.Department ?? "").Trim().ToLowerInvariant(),
            OemStatementGroup = (request.OemStatementGroup ?? "").Trim(),
            IsActive = request.IsActive ?? true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.GlAccounts.Add(entity);
        db.SaveChanges();
        return MapGlAccount(entity);
    }

    public GlEntryRecord CreateGlEntry(CreateGlEntryRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var entity = new DmsGlEntryEntity
        {
            Id = Guid.NewGuid(),
            RepairOrderId = request.RepairOrderId,
            GlAccountId = request.GlAccountId,
            JournalCode = string.IsNullOrWhiteSpace(request.JournalCode) ? "GEN" : request.JournalCode.Trim().ToUpperInvariant(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? "GL posting" : request.Description.Trim(),
            DebitAmount = request.DebitAmount.GetValueOrDefault(),
            CreditAmount = request.CreditAmount.GetValueOrDefault(),
            PostedAtUtc = request.PostedAtUtc ?? DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.GlEntries.Add(entity);
        db.SaveChanges();

        EmitRepairOrderTimeline(db, request.RepairOrderId, "accounting.gl_posted", $"GL posted {entity.JournalCode}", entity.Description, entity.Id);
        return MapGlEntry(entity);
    }

    public AccountsPayableBillRecord CreateAccountsPayableBill(CreateAccountsPayableBillRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var entity = new DmsAccountsPayableBillEntity
        {
            Id = Guid.NewGuid(),
            RepairOrderId = request.RepairOrderId,
            VendorName = string.IsNullOrWhiteSpace(request.VendorName) ? "Vendor" : request.VendorName.Trim(),
            InvoiceNumber = string.IsNullOrWhiteSpace(request.InvoiceNumber) ? $"AP-{DateTime.UtcNow:HHmmss}" : request.InvoiceNumber.Trim().ToUpperInvariant(),
            Amount = request.Amount.GetValueOrDefault(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "open" : request.Status.Trim().ToLowerInvariant(),
            DueAtUtc = request.DueAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.AccountsPayableBills.Add(entity);
        db.SaveChanges();

        EmitRepairOrderTimeline(db, request.RepairOrderId, "accounting.ap_bill_created", $"AP bill {entity.InvoiceNumber}", $"{entity.VendorName} {entity.Amount:0.00}", entity.Id);
        return MapApBill(entity);
    }

    public AccountsReceivableInvoiceRecord CreateAccountsReceivableInvoice(CreateAccountsReceivableInvoiceRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var entity = new DmsAccountsReceivableInvoiceEntity
        {
            Id = Guid.NewGuid(),
            RepairOrderId = request.RepairOrderId,
            CustomerId = request.CustomerId,
            InvoiceNumber = string.IsNullOrWhiteSpace(request.InvoiceNumber) ? $"AR-{DateTime.UtcNow:HHmmss}" : request.InvoiceNumber.Trim().ToUpperInvariant(),
            Amount = request.Amount.GetValueOrDefault(),
            BalanceDue = request.BalanceDue ?? request.Amount.GetValueOrDefault(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "open" : request.Status.Trim().ToLowerInvariant(),
            DueAtUtc = request.DueAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.AccountsReceivableInvoices.Add(entity);

        if (request.RepairOrderId.HasValue)
        {
            db.AccountingEntries.Add(new DmsAccountingEntryEntity
            {
                Id = Guid.NewGuid(),
                RepairOrderId = request.RepairOrderId.Value,
                EntryType = "invoice_posted",
                Description = $"AR invoice {entity.InvoiceNumber}",
                Amount = entity.Amount,
                Status = entity.Status,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        db.SaveChanges();

        var repairOrder = request.RepairOrderId.HasValue
            ? db.RepairOrders.FirstOrDefault(x => x.Id == request.RepairOrderId.Value)
            : null;

        if (repairOrder != null)
        {
            _dmsCore.AddTimelineEvent(new CreateTimelineEventRequest
            {
                CustomerId = repairOrder.CustomerId,
                VehicleId = repairOrder.VehicleId,
                EventType = "accounting.ar_invoice_created",
                Title = $"AR invoice {entity.InvoiceNumber}",
                Body = $"{entity.Amount:0.00} due",
                Department = "accounting",
                SourceSystem = "ingrid.accounting",
                SourceId = entity.Id.ToString()
            });
        }

        return MapArInvoice(entity);
    }

    public BankReconciliationRecord CreateBankReconciliation(CreateBankReconciliationRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var entity = new DmsBankReconciliationEntity
        {
            Id = Guid.NewGuid(),
            AccountNumber = string.IsNullOrWhiteSpace(request.AccountNumber) ? "OPERATING" : request.AccountNumber.Trim().ToUpperInvariant(),
            StatementEndingAtUtc = request.StatementEndingAtUtc ?? DateTime.UtcNow,
            StatementBalance = request.StatementBalance.GetValueOrDefault(),
            BookBalance = request.BookBalance.GetValueOrDefault(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "open" : request.Status.Trim().ToLowerInvariant(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.BankReconciliations.Add(entity);
        db.SaveChanges();
        return MapBankRec(entity);
    }

    public AccountingClosePeriodRecord CreateClosePeriod(CreateAccountingClosePeriodRequest request)
    {
        using var db = _dbContextFactory.CreateDbContext();
        var entity = new DmsAccountingClosePeriodEntity
        {
            Id = Guid.NewGuid(),
            PeriodName = string.IsNullOrWhiteSpace(request.PeriodName) ? DateTime.UtcNow.ToString("yyyy-MM") : request.PeriodName.Trim(),
            PeriodStartUtc = request.PeriodStartUtc ?? DateTime.UtcNow.Date,
            PeriodEndUtc = request.PeriodEndUtc ?? DateTime.UtcNow.Date,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "open" : request.Status.Trim().ToLowerInvariant(),
            Notes = (request.Notes ?? "").Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.AccountingClosePeriods.Add(entity);
        db.SaveChanges();
        return MapClosePeriod(entity);
    }

    private void EmitRepairOrderTimeline(IngridDmsDbContext db, Guid? repairOrderId, string eventType, string title, string body, Guid sourceId)
    {
        if (!repairOrderId.HasValue)
        {
            return;
        }

        var repairOrder = db.RepairOrders.FirstOrDefault(x => x.Id == repairOrderId.Value);
        if (repairOrder == null)
        {
            return;
        }

        _dmsCore.AddTimelineEvent(new CreateTimelineEventRequest
        {
            CustomerId = repairOrder.CustomerId,
            VehicleId = repairOrder.VehicleId,
            EventType = eventType,
            Title = title,
            Body = body,
            Department = "accounting",
            SourceSystem = "ingrid.accounting",
            SourceId = sourceId.ToString()
        });
    }

    private static GlAccountRecord MapGlAccount(DmsGlAccountEntity entity)
    {
        return new GlAccountRecord
        {
            Id = entity.Id,
            AccountNumber = entity.AccountNumber,
            Description = entity.Description,
            AccountType = entity.AccountType,
            Department = entity.Department,
            OemStatementGroup = entity.OemStatementGroup,
            IsActive = entity.IsActive,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }

    private static GlEntryRecord MapGlEntry(DmsGlEntryEntity entity)
    {
        return new GlEntryRecord
        {
            Id = entity.Id,
            RepairOrderId = entity.RepairOrderId,
            GlAccountId = entity.GlAccountId,
            JournalCode = entity.JournalCode,
            Description = entity.Description,
            DebitAmount = entity.DebitAmount,
            CreditAmount = entity.CreditAmount,
            PostedAtUtc = entity.PostedAtUtc,
            CreatedAtUtc = entity.CreatedAtUtc
        };
    }

    private static AccountsPayableBillRecord MapApBill(DmsAccountsPayableBillEntity entity)
    {
        return new AccountsPayableBillRecord
        {
            Id = entity.Id,
            RepairOrderId = entity.RepairOrderId,
            VendorName = entity.VendorName,
            InvoiceNumber = entity.InvoiceNumber,
            Amount = entity.Amount,
            Status = entity.Status,
            DueAtUtc = entity.DueAtUtc,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }

    private static AccountsReceivableInvoiceRecord MapArInvoice(DmsAccountsReceivableInvoiceEntity entity)
    {
        return new AccountsReceivableInvoiceRecord
        {
            Id = entity.Id,
            RepairOrderId = entity.RepairOrderId,
            CustomerId = entity.CustomerId,
            InvoiceNumber = entity.InvoiceNumber,
            Amount = entity.Amount,
            BalanceDue = entity.BalanceDue,
            Status = entity.Status,
            DueAtUtc = entity.DueAtUtc,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }

    private static BankReconciliationRecord MapBankRec(DmsBankReconciliationEntity entity)
    {
        return new BankReconciliationRecord
        {
            Id = entity.Id,
            AccountNumber = entity.AccountNumber,
            StatementEndingAtUtc = entity.StatementEndingAtUtc,
            StatementBalance = entity.StatementBalance,
            BookBalance = entity.BookBalance,
            Status = entity.Status,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }

    private static AccountingClosePeriodRecord MapClosePeriod(DmsAccountingClosePeriodEntity entity)
    {
        return new AccountingClosePeriodRecord
        {
            Id = entity.Id,
            PeriodName = entity.PeriodName,
            PeriodStartUtc = entity.PeriodStartUtc,
            PeriodEndUtc = entity.PeriodEndUtc,
            Status = entity.Status,
            Notes = entity.Notes,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }
}
