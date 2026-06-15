namespace HypeGrid.API.Controllers.Admin;

/// <summary>Body for status-change PATCH endpoints.</summary>
public sealed class StatusPatchDto
{
    public string Status { get; set; } = string.Empty;
}

/// <summary>Body for assignment PATCH endpoints.</summary>
public sealed class AssignPatchDto
{
    public Guid? AssignedToUserId { get; set; }
}

/// <summary>Body for campaign progress PATCH.</summary>
public sealed class ProgressPatchDto
{
    public int Progress { get; set; }
}

/// <summary>Body for recording a payment against an invoice.</summary>
public sealed class RecordPaymentDto
{
    public decimal Amount { get; set; }
}

/// <summary>Body for appending an admin note to a lead.</summary>
public sealed class AppendNoteDto
{
    public string Content { get; set; } = string.Empty;
}

/// <summary>Body for active/inactive toggle PATCH endpoints.</summary>
public sealed class ActivePatchDto
{
    public bool IsActive { get; set; }
}

/// <summary>Body for featured/unfeatured toggle PATCH endpoints.</summary>
public sealed class FeaturedPatchDto
{
    public bool IsFeatured { get; set; }
}
