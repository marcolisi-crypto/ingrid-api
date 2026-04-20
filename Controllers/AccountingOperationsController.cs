using AIReception.Mvc.Models.Dms;
using AIReception.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIReception.Mvc.Controllers;

[ApiController]
[Route("api/accounting")]
public class AccountingOperationsController : ControllerBase
{
    private readonly AccountingOperationsService _accountingOperations;

    public AccountingOperationsController(AccountingOperationsService accountingOperations)
    {
        _accountingOperations = accountingOperations;
    }

    [HttpGet("gl/accounts")]
    public IActionResult GetGlAccounts()
    {
        return Ok(new { accounts = _accountingOperations.GetGlAccounts() });
    }

    [HttpPost("gl/accounts")]
    public IActionResult CreateGlAccount([FromBody] CreateGlAccountRequest request)
    {
        return Ok(_accountingOperations.CreateGlAccount(request));
    }

    [HttpGet("gl/entries")]
    public IActionResult GetGlEntries([FromQuery] Guid? repairOrderId)
    {
        return Ok(new { entries = _accountingOperations.GetGlEntries(repairOrderId) });
    }

    [HttpPost("gl/entries")]
    public IActionResult CreateGlEntry([FromBody] CreateGlEntryRequest request)
    {
        return Ok(_accountingOperations.CreateGlEntry(request));
    }

    [HttpGet("ap/bills")]
    public IActionResult GetAccountsPayableBills()
    {
        return Ok(new { bills = _accountingOperations.GetAccountsPayableBills() });
    }

    [HttpPost("ap/bills")]
    public IActionResult CreateAccountsPayableBill([FromBody] CreateAccountsPayableBillRequest request)
    {
        return Ok(_accountingOperations.CreateAccountsPayableBill(request));
    }

    [HttpGet("ar/invoices")]
    public IActionResult GetAccountsReceivableInvoices([FromQuery] Guid? repairOrderId)
    {
        return Ok(new { invoices = _accountingOperations.GetAccountsReceivableInvoices(repairOrderId) });
    }

    [HttpPost("ar/invoices")]
    public IActionResult CreateAccountsReceivableInvoice([FromBody] CreateAccountsReceivableInvoiceRequest request)
    {
        return Ok(_accountingOperations.CreateAccountsReceivableInvoice(request));
    }

    [HttpGet("bank-reconciliations")]
    public IActionResult GetBankReconciliations()
    {
        return Ok(new { reconciliations = _accountingOperations.GetBankReconciliations() });
    }

    [HttpPost("bank-reconciliations")]
    public IActionResult CreateBankReconciliation([FromBody] CreateBankReconciliationRequest request)
    {
        return Ok(_accountingOperations.CreateBankReconciliation(request));
    }

    [HttpGet("close-periods")]
    public IActionResult GetClosePeriods()
    {
        return Ok(new { periods = _accountingOperations.GetClosePeriods() });
    }

    [HttpPost("close-periods")]
    public IActionResult CreateClosePeriod([FromBody] CreateAccountingClosePeriodRequest request)
    {
        return Ok(_accountingOperations.CreateClosePeriod(request));
    }

    [HttpGet("wip")]
    public IActionResult GetWorkInProgress([FromQuery] Guid? repairOrderId)
    {
        return Ok(new { items = _accountingOperations.GetWorkInProgress(repairOrderId) });
    }

    [HttpPost("wip")]
    public IActionResult CreateWorkInProgress([FromBody] CreateWorkInProgressRequest request)
    {
        return Ok(_accountingOperations.CreateWorkInProgress(request));
    }
}
