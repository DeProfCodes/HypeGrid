using HypeGrid.Application.Leads.Dtos;
using HypeGrid.Shared.Results;

namespace HypeGrid.Application.Leads;

/// <summary>
/// Handles the four public-website conversion paths: contact, campaign request,
/// creator application, and newsletter. Each method validates, persists, and
/// triggers the relevant email fan-out.
/// </summary>
public interface IPublicLeadService
{
    Task<Result<Guid>> SubmitContactAsync(ContactFormDto dto, CancellationToken ct = default);
    Task<Result<Guid>> SubmitCampaignRequestAsync(CampaignRequestFormDto dto, CancellationToken ct = default);
    Task<Result<Guid>> SubmitCreatorApplicationAsync(CreatorApplicationFormDto dto, CancellationToken ct = default);
    Task<Result<Guid>> SubscribeNewsletterAsync(NewsletterSubscribeDto dto, CancellationToken ct = default);
    Task<Result> UnsubscribeNewsletterAsync(string email, CancellationToken ct = default);
}
