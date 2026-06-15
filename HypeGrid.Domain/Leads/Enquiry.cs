using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Leads;

/// <summary>
/// A general contact / enquiry submitted from the public website Contact form
/// (HypeGridWebsite/src/pages/Contact.jsx). Statuses match
/// <c>HypeGridValues.EnquiryStatuses</c>.
/// </summary>
public class Enquiry : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Subject { get; set; }
    public string? Message { get; set; }

    /// <summary>Maps to the website form's "interest" dropdown.</summary>
    public string? InterestType { get; set; }

    /// <summary>The page the enquiry was submitted from (e.g. "contact").</summary>
    public string? SourcePage { get; set; }

    public string Status { get; set; } = "New";
    public Guid? AssignedToUserId { get; set; }
    public string? AdminNotes { get; set; }
}
