using HypeGrid.Domain.Common;

namespace HypeGrid.Domain.Content;

/// <summary>
/// A single key/value site setting (company name, support email, social links,
/// auto-response templates, etc.). Powers the admin Settings page, which is
/// currently fully hardcoded in HypeGridAdmin/src/pages/Settings.jsx.
/// One row per key; <see cref="Group"/> buckets keys for the settings tabs
/// (company / site / communication).
/// </summary>
public class SiteSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string Group { get; set; } = "company";
    public string? Description { get; set; }
}
