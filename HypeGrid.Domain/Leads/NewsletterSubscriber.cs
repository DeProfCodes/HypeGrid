using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Leads;

/// <summary>
/// A newsletter / lead-capture subscriber. Email is unique; a duplicate
/// subscribe re-activates an existing inactive record rather than inserting.
/// </summary>
public class NewsletterSubscriber : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? SourcePage { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UnsubscribedAt { get; set; }
}
