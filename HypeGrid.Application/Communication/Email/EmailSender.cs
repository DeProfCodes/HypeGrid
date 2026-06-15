namespace HypeGrid.Application.Communication.Email;

/// <summary>
/// Logical sender identities, each mapped to its own SMTP account/from-address
/// via configuration. Mirrors the ZansiHustle six-identity sender pool, scoped
/// to the identities HypeGrid actually uses.
/// </summary>
public enum EmailSender
{
    Default = 0,
    Support = 1,
    Campaigns = 2,
    Creators = 3,
    NoReply = 4
}
