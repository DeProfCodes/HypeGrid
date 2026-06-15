namespace HypeGrid.Shared.Constants;

/// <summary>
/// Allowed string values for status / type fields, kept verbatim to match the
/// HypeGrid website + admin frontends (Base44 entity enums). Statuses and types
/// are persisted as strings — NOT C# enums — because several contain spaces and
/// slashes (e.g. "Song / Music Release") and the frontend renders/filters on the
/// exact literal. Treat these arrays as the single source of truth for both
/// validation and dropdown seeding.
/// </summary>
public static class HypeGridValues
{
    // ── Enquiry (general contact) ───────────────────────────────────────────
    public static readonly string[] EnquiryStatuses =
    {
        "New", "Contacted", "Qualified", "Quote Needed",
        "Converted", "Not Suitable", "Closed", "Archived"
    };

    public static readonly string[] EnquiryInterestTypes =
    {
        "Starting a Campaign", "Joining the Creator Network", "Social Media Management",
        "Music Promotion", "Event Promotion", "Business Promotion", "Other"
    };

    // ── Campaign request / Campaign ─────────────────────────────────────────
    public static readonly string[] CampaignTypes =
    {
        "Business Promotion", "Song / Music Release", "Event Promotion",
        "Product Promotion", "App / Startup Launch", "Influencer Campaign",
        "Social Media Management", "Other"
    };

    public static readonly string[] CampaignRequestStatuses =
    {
        "New", "Contacted", "Qualified", "Quote Needed",
        "Converted", "Not Suitable", "Closed"
    };

    public static readonly string[] CampaignStatuses =
    {
        "Draft", "Requested", "Quoted", "Awaiting Payment", "Approved",
        "In Planning", "Active", "Content Pending", "Client Review",
        "Completed", "Cancelled"
    };

    public static readonly string[] CampaignPhases =
    {
        "Request Received", "Quote Sent", "Payment Confirmed", "Strategy / Planning",
        "Content Creation", "Creator Activation", "Campaign Live", "Reporting", "Completed"
    };

    // ── Client ──────────────────────────────────────────────────────────────
    public static readonly string[] ClientTypes =
    {
        "Business", "Artist / Musician", "Event Organizer", "Startup / App",
        "Product Brand", "Personal Brand", "Other"
    };

    public static readonly string[] ClientStatuses =
    {
        "Lead", "Active", "Inactive", "VIP", "Archived"
    };

    // ── Creator / Creator application ───────────────────────────────────────
    public static readonly string[] CreatorPlatforms =
    {
        "TikTok", "Instagram", "Facebook", "YouTube", "WhatsApp", "Other"
    };

    public static readonly string[] CreatorNiches =
    {
        "Lifestyle", "Music", "Comedy", "Fashion", "Beauty", "Business",
        "Fitness", "Food", "Events", "Tech", "Other"
    };

    public static readonly string[] CreatorStatuses =
    {
        "Applied", "Under Review", "Approved", "Active", "Suspended", "Rejected", "Archived"
    };

    // ── Deliverable ─────────────────────────────────────────────────────────
    public static readonly string[] DeliverableTypes =
    {
        "Reel", "TikTok Video", "Instagram Post", "Story", "Flyer",
        "Motion Graphic", "Caption", "Influencer Post", "YouTube Short", "Campaign Report"
    };

    public static readonly string[] DeliverableStatuses =
    {
        "Not Started", "In Progress", "Uploaded", "Needs Changes",
        "Approved", "Posted", "Rejected"
    };

    // ── Task ────────────────────────────────────────────────────────────────
    public static readonly string[] TaskPriorities = { "Low", "Medium", "High", "Urgent" };

    public static readonly string[] TaskStatuses =
    {
        "To Do", "In Progress", "Waiting for Client", "Waiting for Creator", "Review", "Done"
    };

    // ── Note ────────────────────────────────────────────────────────────────
    public static readonly string[] NoteEntityTypes = { "Campaign", "Client", "Creator", "Internal" };
    public static readonly string[] NoteVisibilities = { "Internal", "Client-facing" };

    // ── Finance ─────────────────────────────────────────────────────────────
    public static readonly string[] QuotePackages =
    {
        "Starter Hype", "Growth Hype", "Premium Launch",
        "Monthly Brand Management", "Music Release Push", "Custom Campaign"
    };

    public static readonly string[] QuoteStatuses =
    {
        "Draft", "Sent", "Accepted", "Rejected", "Expired", "Converted to Invoice"
    };

    public static readonly string[] InvoiceStatuses =
    {
        "Draft", "Sent", "Paid", "Partially Paid", "Overdue", "Cancelled"
    };

    public static readonly string[] PayoutStatuses =
    {
        "Pending", "Approved", "Paid", "On Hold", "Cancelled"
    };

    // ── Marketing: deals / hero placements ──────────────────────────────────
    public static readonly string[] DealCategories =
    {
        "Food", "Grocery", "Fashion", "Beauty", "Tech", "Mobile", "Home",
        "Events", "Music", "Travel", "Services", "Apps", "Other"
    };

    /// <summary>How a hero/CTA link should be interpreted by the website.</summary>
    public static readonly string[] CtaTargetTypes =
    {
        "internal", "external", "whatsapp", "deal", "campaign"
    };

    // ── Alerts / Deals Club (consent-based audience opt-in) ─────────────────
    public static readonly string[] AlertSubscriberStatuses =
    {
        "New", "Active", "OptedOut", "Archived"
    };

    public static readonly string[] AlertFrequencies =
    {
        "Daily", "Weekly", "Weekends only", "Best deals only"
    };

    /// <summary>South African provinces — for the public alerts opt-in dropdown.</summary>
    public static readonly string[] SaProvinces =
    {
        "Eastern Cape", "Free State", "Gauteng", "KwaZulu-Natal", "Limpopo",
        "Mpumalanga", "Northern Cape", "North West", "Western Cape"
    };

    // ── Analytics ───────────────────────────────────────────────────────────
    public static readonly string[] AnalyticsEventTypes =
    {
        "impression", "cta_click", "card_view", "card_click",
        "whatsapp_click", "external_click", "video_impression", "video_cta_click",
        "alert_subscribe"
    };

    public static readonly string[] AnalyticsEntityTypes =
    {
        "HeroPlacement", "Deal", "FeaturedVideo", "Campaign", "Service", "Package",
        "AlertSubscription", "Other"
    };
}
