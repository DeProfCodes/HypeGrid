namespace HypeGrid.Domain.Finance;

/// <summary>
/// A line item embedded in a <see cref="Quote"/> or <see cref="Invoice"/>.
/// Persisted as a JSON column (matches the Base44 array-of-objects shape).
/// </summary>
public sealed class LineItem
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
