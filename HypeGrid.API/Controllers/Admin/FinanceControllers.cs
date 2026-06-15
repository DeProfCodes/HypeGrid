using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using HypeGrid.Application.Common.Interfaces;
using HypeGrid.Domain.Finance;

namespace HypeGrid.API.Controllers.Admin;

/// <summary>Quotes — backs the admin Quotes page + convert-to-invoice.</summary>
[Route("api/admin/quotes")]
public sealed class QuotesController : AdminCrudController<Quote>
{
    private readonly IRepository<Invoice> _invoices;

    public QuotesController(IRepository<Quote> repo, IRepository<Invoice> invoices) : base(repo)
        => _invoices = invoices;

    protected override Expression<Func<Quote, bool>> BuildSearchPredicate(string q)
        => qt => (qt.ClientName != null && qt.ClientName.Contains(q)) || qt.QuoteNumber.Contains(q);

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] StatusPatchDto dto, CancellationToken ct)
    {
        var quote = await Repo.GetByIdAsync(id, ct);
        if (quote is null) return MapNotFound();
        quote.Status = dto.Status;
        Repo.Update(quote);
        await Repo.SaveChangesAsync(ct);
        return Data(quote, "Status updated.");
    }

    [HttpPost("{id:guid}/convert-to-invoice")]
    public async Task<IActionResult> ConvertToInvoice(Guid id, CancellationToken ct)
    {
        var quote = await Repo.GetByIdAsync(id, ct);
        if (quote is null) return MapNotFound();

        var invoice = new Invoice
        {
            InvoiceNumber = quote.QuoteNumber.Replace("HG-Q", "HG-INV"),
            ClientId = quote.ClientId,
            ClientName = quote.ClientName,
            Amount = quote.Amount,
            Outstanding = quote.Amount,
            PaidAmount = 0,
            Status = "Draft",
            LineItems = quote.LineItems.Select(li => new LineItem { Description = li.Description, Amount = li.Amount }).ToList()
        };
        await _invoices.AddAsync(invoice, ct);

        quote.Status = "Converted to Invoice";
        Repo.Update(quote);
        await _invoices.SaveChangesAsync(ct);

        return Data(invoice, "Converted to invoice.");
    }
}

/// <summary>Invoices — backs the admin Payments page + record-payment.</summary>
[Route("api/admin/invoices")]
public sealed class InvoicesController : AdminCrudController<Invoice>
{
    public InvoicesController(IRepository<Invoice> repo) : base(repo) { }

    protected override Expression<Func<Invoice, bool>> BuildSearchPredicate(string q)
        => i => (i.ClientName != null && i.ClientName.Contains(q)) || i.InvoiceNumber.Contains(q);

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] StatusPatchDto dto, CancellationToken ct)
    {
        var invoice = await Repo.GetByIdAsync(id, ct);
        if (invoice is null) return MapNotFound();
        invoice.Status = dto.Status;
        Repo.Update(invoice);
        await Repo.SaveChangesAsync(ct);
        return Data(invoice, "Status updated.");
    }

    [HttpPost("{id:guid}/record-payment")]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPaymentDto dto, CancellationToken ct)
    {
        var invoice = await Repo.GetByIdAsync(id, ct);
        if (invoice is null) return MapNotFound();

        invoice.PaidAmount += dto.Amount;
        invoice.Outstanding = Math.Max(0, invoice.Amount - invoice.PaidAmount);
        invoice.Status = invoice.Outstanding <= 0 ? "Paid" : "Partially Paid";

        Repo.Update(invoice);
        await Repo.SaveChangesAsync(ct);
        return Data(invoice, "Payment recorded.");
    }
}

/// <summary>Payouts — backs the admin Payouts page + approve / mark-paid.</summary>
[Route("api/admin/payouts")]
public sealed class PayoutsController : AdminCrudController<Payout>
{
    public PayoutsController(IRepository<Payout> repo) : base(repo) { }

    protected override Expression<Func<Payout, bool>> BuildSearchPredicate(string q)
        => p => p.CreatorName != null && p.CreatorName.Contains(q);

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] StatusPatchDto dto, CancellationToken ct)
    {
        var payout = await Repo.GetByIdAsync(id, ct);
        if (payout is null) return MapNotFound();
        payout.Status = dto.Status;
        Repo.Update(payout);
        await Repo.SaveChangesAsync(ct);
        return Data(payout, "Status updated.");
    }

    [HttpPost("{id:guid}/mark-paid")]
    public async Task<IActionResult> MarkPaid(Guid id, CancellationToken ct)
    {
        var payout = await Repo.GetByIdAsync(id, ct);
        if (payout is null) return MapNotFound();
        payout.Status = "Paid";
        payout.PaidDate = DateTime.UtcNow;
        Repo.Update(payout);
        await Repo.SaveChangesAsync(ct);
        return Data(payout, "Marked as paid.");
    }
}
