# INGRID Accounting Standards Map

This document maps the current INGRID accounting module to the standards in:

- `Retailer Accounting Manual 2019 - 08_01_2019.pdf`

The manual is being treated as the source of truth for how the dealership accounting module should be structured. The goal is not to build a generic AP/AR shell, but an accounting system that follows retailer accounting, profit-centre reporting, balance-sheet classification, and aftersales transaction treatment expected in the attached standard.

## What The Manual Tells Us

The manual establishes a few non-negotiable design rules:

1. Accounting is profit-centre based.
Service, Parts, Body & Paint, New Vehicles, Used Vehicles, F&I, and unallocated overhead are reported separately.

2. OEM-style statement grouping matters.
Accounts are not just freeform GL lines. They need mapping into statement sections like revenue, cost of sales, contribution layers, balance sheet sections, stock, liabilities, and supplemental KPIs.

3. Aftersales accounting must separate customer pay, warranty, internal, maintenance, and sublet activity.
This affects RO posting, invoices, WIP, receivables, payables, and reporting.

4. Balance sheet structure is explicit.
The manual separates receivables, stock, work in progress, payables, accrued liabilities, taxes, and provisions into named buckets.

5. Inventory and parts aging are first-class accounting data.
Parts stock, stock write-downs, holding days, and WIP are part of the financial model, not just operational inventory screens.

6. Accounting needs close controls and reconciliations.
Bank rec, period close, accruals, provisions, and financial statement output are part of the required system, not optional back-office extras.

## Manual Sections That Matter Most

These are the sections that should directly drive the implementation:

- `1.3` Accounting, financial statements, management accounts
- `1.4` IFC overview, brands, profit centres, P&L summary
- `2.6` Service
- `2.8` Parts
- `8` Balance Sheet
- `9.2` Parts stock
- `11.1` Aftersales orders
- `16.3` Service revenue and cost of sales
- `16.5` Parts revenue and cost of sales
- `19.5` Workshop and parts sales
- `12` KPI calculation

Key details extracted from the manual:

- Receivables should distinguish vehicle, aftersales, warranty, F&I, bonuses/support, external, internal, taxes, and bad debt provision.
- Stock should distinguish new vehicles, used vehicles, parts, work in progress, and stock write-down provisions.
- Short-term payables should distinguish BMW Group dealership operations by area:
  - new vehicles
  - used vehicles
  - service / body & paint
  - parts
  - other suppliers
- Accrued liabilities should distinguish:
  - customer advances
  - unearned bonuses/support
  - employees
  - payroll/social liabilities
  - pension
  - interest
  - taxes
  - other accruals/deferrals
- Workshop and parts sales must support:
  - customer pay
  - warranty claims
  - maintenance agreements
  - sublet repairs
  - inventory adjustments

## Current INGRID Accounting Status

Today the backend already has a useful foundation:

- general ledger accounts
- general ledger entries
- accounts payable bills
- accounts receivable invoices
- bank reconciliations
- close periods

Relevant code:

- [Services/AccountingOperationsService.cs](/Users/marcolisi/Documents/Codex/2026-04-17-files-mentioned-by-the-user-ait/ai-reception-csharp/AI-Reception-CSharp-main/Services/AccountingOperationsService.cs)
- [Controllers/AccountingOperationsController.cs](/Users/marcolisi/Documents/Codex/2026-04-17-files-mentioned-by-the-user-ait/ai-reception-csharp/AI-Reception-CSharp-main/Controllers/AccountingOperationsController.cs)
- [Data/Entities/DmsPersistenceEntities.cs](/Users/marcolisi/Documents/Codex/2026-04-17-files-mentioned-by-the-user-ait/ai-reception-csharp/AI-Reception-CSharp-main/Data/Entities/DmsPersistenceEntities.cs)
- [Models/Dms/DmsModels.cs](/Users/marcolisi/Documents/Codex/2026-04-17-files-mentioned-by-the-user-ait/ai-reception-csharp/AI-Reception-CSharp-main/Models/Dms/DmsModels.cs)

What is still missing versus the manual:

- no OEM-style chart-of-accounts template
- no required statement grouping hierarchy
- no receivables aging by type
- no payables aging/classification by supplier group
- no service/parts WIP accounting object
- no warranty receivable / warranty accrual model
- no customer/internal/warranty split posting model at invoice level
- no stock write-down / obsolescence accounting for parts
- no financial statement output
- no month-end checklist / close-lock behavior beyond basic close period records

## Required Accounting Domain Model

To match the manual, INGRID should treat these as first-class accounting objects.

### 1. Chart Of Accounts

Every GL account should have:

- `accountNumber`
- `description`
- `accountType`
- `profitCentre`
- `brand`
- `statementSection`
- `statementSubsection`
- `oemStatementGroup`
- `isControlAccount`
- `isActive`

Recommended profit centres:

- `new_vehicle`
- `used_vehicle`
- `service`
- `body_paint`
- `parts`
- `fi`
- `admin`
- `unallocated`

Recommended statement sections:

- `balance_sheet.asset.current`
- `balance_sheet.asset.non_current`
- `balance_sheet.liability.current`
- `balance_sheet.liability.non_current`
- `balance_sheet.equity`
- `pnl.revenue`
- `pnl.cost_of_sales`
- `pnl.contribution_ii`
- `pnl.contribution_iii`
- `pnl.overheads`
- `pnl.depreciation`
- `pnl.interest`
- `pnl.tax`
- `supplementary`

### 2. Receivables

AR invoices are not enough by themselves. We need receivable classification and aging.

Required receivable classes:

- `vehicle`
- `aftersales`
- `warranty`
- `fi`
- `bonus_support`
- `external_other`
- `internal_other`
- `tax`

Required fields:

- `receivableType`
- `customerId`
- `repairOrderId`
- `invoiceId`
- `agingBucket`
- `originalAmount`
- `balanceDue`
- `dueAtUtc`
- `postedAtUtc`
- `brand`
- `profitCentre`

### 3. Payables

AP needs supplier-group classification aligned to the manual.

Required payable classes:

- `new_vehicle`
- `used_vehicle`
- `service_body_paint`
- `parts`
- `other_supplier`
- `external_other`
- `internal_other`

Required fields:

- `payableType`
- `vendorName`
- `vendorAccount`
- `repairOrderId`
- `purchaseOrderId`
- `invoiceNumber`
- `amount`
- `balanceDue`
- `dueAtUtc`
- `postedAtUtc`
- `brand`
- `profitCentre`

### 4. Aftersales WIP

The manual explicitly expects work in progress for parts and hours.

We need a WIP object tied to the RO with:

- labour value not yet invoiced
- parts value not yet invoiced
- sublet value not yet invoiced
- pay split
- warranty/internal/customer classification
- posted/unposted state

### 5. Warranty Accounting

Warranty cannot stay just an RO label. It needs accounting treatment.

We need:

- `WarrantyClaim`
- `WarrantyReceivable`
- `WarrantyAccrual`
- optional `GoodwillExpense`

Required fields:

- `claimNumber`
- `repairOrderId`
- `manufacturer`
- `claimAmount`
- `approvedAmount`
- `receivableStatus`
- `submittedAtUtc`
- `approvedAtUtc`
- `postedAtUtc`

### 6. Parts Inventory Accounting

The manual expects parts inventory, WIP, and write-down logic.

We need:

- inventory value by brand
- bin/location operations tied to valuation
- obsolescence / write-down entries
- stock adjustments
- holding days support
- category support:
  - parts
  - accessories
  - lifestyle / riders gear
  - tires
  - oil / lubricants
  - other

### 7. Financial Statements

We should plan explicit outputs for:

- OEM-style P&L by contribution layers
- balance sheet
- receivables aging
- payables aging
- stock valuation and holding days
- service / parts supplementary aftersales order reporting

## How The Service, Parts, And Accounting Modules Should Connect

### Service

Service should create accounting-relevant records, not just operational records.

Flow:

1. `Appointment`
2. `ServiceReception`
3. `RepairOrder`
4. `ServiceQuote`
5. `LabourOps + Parts + MPI + Warranty`
6. `ServiceInvoice`
7. `Receivable + GL posting + WIP release`

### Parts

Parts should create both operational and accounting events.

Flow:

1. `PartRequest / Picking / SpecialOrder`
2. `Inventory allocation`
3. `Cost impact`
4. `PartsQuote or direct parts sale`
5. `PartsInvoice`
6. `AR / GL posting`

### Accounting

Accounting should consume posted operational events, not manually recreate them.

Accounting actions should include:

- post service invoice
- post parts invoice
- create AP bill
- reconcile bank
- review and clear receivables
- close accounting period
- produce statement output

## Recommended Build Order

### Phase 1: Normalize the accounting schema

Add the missing accounting classifications:

- GL statement sections
- profit centre
- receivable type
- payable type
- brand
- aging buckets
- posting status

### Phase 2: Make service and parts post correctly

Add:

- service invoice posting
- parts invoice posting
- customer/internal/warranty split posting
- WIP tracking

### Phase 3: Add warranty and provisions

Add:

- warranty claims
- warranty receivables
- warranty/goodwill accruals
- parts obsolescence / stock write-downs

### Phase 4: Add statement and close outputs

Add:

- balance sheet output
- P&L output
- AP aging
- AR aging
- stock valuation report
- month-end close checks

## Concrete Backend Changes To Make Next

These are the next backend additions that should happen if we want to follow the manual faithfully:

1. Extend `DmsGlAccountEntity`
- add `ProfitCentre`
- add `Brand`
- add `StatementSection`
- add `StatementSubsection`
- tighten `OemStatementGroup`

2. Extend `DmsAccountsReceivableInvoiceEntity`
- add `ReceivableType`
- add `Brand`
- add `ProfitCentre`
- add `AgingBucket`
- add `PostedAtUtc`

3. Extend `DmsAccountsPayableBillEntity`
- add `PayableType`
- add `Brand`
- add `ProfitCentre`
- add `AgingBucket`
- add `PostedAtUtc`

4. Add `DmsWorkInProgressEntity`
- split labour/parts/sublet
- tie to `RepairOrderId`
- track `PayType`

5. Add `DmsWarrantyClaimEntity`
- claim status
- approved amount
- receivable linkage

6. Add `DmsStatementSnapshotEntity`
- period
- statement type
- generated JSON payload

## Concrete Frontend Changes To Make Next

Accounting screens should stop being generic “open balance / review” boards and become standards-based dashboards:

- `GL`
  - accounts by statement section
  - post journal
- `AR`
  - aging buckets
  - aftersales vs warranty vs other
- `AP`
  - supplier group buckets
  - due aging
- `Service Accounting`
  - open WIP
  - unposted service invoices
  - warranty receivables
- `Parts Accounting`
  - inventory value
  - obsolescence / write-down
  - special-order liabilities
- `Close`
  - bank rec
  - accrual checks
  - period close checklist

## Practical Rule For INGRID

From this point on, any accounting feature should pass this test:

- Can it be mapped to a profit centre?
- Can it be mapped to a statement section?
- Can it be aged or reconciled?
- Can it distinguish customer, internal, warranty, and parts/service classification where required?

If the answer is no, it is not yet aligned to the attached retailer accounting standard.
