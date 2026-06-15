using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Campaigns;

/// <summary>
/// A task on a campaign board. Mirrors the Base44 <c>Task</c> entity (renamed
/// <c>CampaignTask</c> in C# to avoid clashing with
/// <see cref="System.Threading.Tasks.Task"/>; the DB table is "Tasks" and the
/// JSON shape is unchanged).
/// </summary>
public class CampaignTask : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public Guid? CampaignId { get; set; }
    public string? CampaignName { get; set; }

    public string? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "To Do";
    public string? Notes { get; set; }
}
