namespace HypeGrid.Application.Alerts.Dtos;

/// <summary>
/// Public HypeGrid Alerts / Deals Club opt-in submission
/// (HypeGridWebsite/src/pages/Alerts.jsx). The user actively submits this and
/// must tick the consent box (<see cref="Consent"/> = true).
/// </summary>
public sealed class AlertSubscribeDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public List<string> Interests { get; set; } = new();
    public string? FrequencyPreference { get; set; }
    public string? SourcePage { get; set; }

    /// <summary>Must be true — the user actively agreed to the consent text.</summary>
    public bool Consent { get; set; }

    /// <summary>Version tag of the consent copy shown, e.g. "hypegrid-alerts-v1".</summary>
    public string? ConsentVersion { get; set; }

    /// <summary>The exact consent text shown to the user (stored for POPIA proof).</summary>
    public string? ConsentSnapshot { get; set; }
}

/// <summary>Returned after a successful opt-in so the UI can render the WhatsApp CTA.</summary>
public sealed class AlertSubscribeResultDto
{
    public Guid Id { get; set; }
    public string WhatsappChannelUrl { get; set; } = string.Empty;
}

/// <summary>Returned by the channel-click endpoint so the UI can open the channel.</summary>
public sealed class AlertChannelClickResultDto
{
    public string WhatsappChannelUrl { get; set; } = string.Empty;
}

/// <summary>Public opt-out by email and/or phone.</summary>
public sealed class AlertUnsubscribeDto
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

/// <summary>Static option lists for the public alerts form (optional convenience endpoint).</summary>
public sealed class AlertOptionsDto
{
    public IReadOnlyList<string> Interests { get; set; } = new List<string>();
    public IReadOnlyList<string> Frequencies { get; set; } = new List<string>();
    public IReadOnlyList<string> Provinces { get; set; } = new List<string>();
}
