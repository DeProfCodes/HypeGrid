using Microsoft.Extensions.Logging;
using HypeGrid.Application.Common.Interfaces;
using HypeGrid.Application.Common.Validation;
using HypeGrid.Application.Communication.Email.Interfaces;
using HypeGrid.Application.Leads.Dtos;
using HypeGrid.Domain.Leads;
using HypeGrid.Shared.Errors;
using HypeGrid.Shared.Results;

namespace HypeGrid.Application.Leads;

/// <summary>
/// Default implementation of the public conversion paths. Validation is
/// intentionally lenient — the public forms only mark a few fields required —
/// but email shape is always checked. Persistence happens before email so a
/// mail outage never loses a lead.
/// </summary>
public sealed class PublicLeadService : IPublicLeadService
{
    private readonly IRepository<Enquiry> _enquiries;
    private readonly IRepository<CampaignRequest> _campaignRequests;
    private readonly IRepository<CreatorApplication> _creatorApplications;
    private readonly IRepository<NewsletterSubscriber> _subscribers;
    private readonly IEmailService _email;
    private readonly ILogger<PublicLeadService> _logger;

    public PublicLeadService(
        IRepository<Enquiry> enquiries,
        IRepository<CampaignRequest> campaignRequests,
        IRepository<CreatorApplication> creatorApplications,
        IRepository<NewsletterSubscriber> subscribers,
        IEmailService email,
        ILogger<PublicLeadService> logger)
    {
        _enquiries = enquiries;
        _campaignRequests = campaignRequests;
        _creatorApplications = creatorApplications;
        _subscribers = subscribers;
        _email = email;
        _logger = logger;
    }

    public async Task<Result<Guid>> SubmitContactAsync(ContactFormDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return Result<Guid>.Failure(ErrorCodes.Validation, "Full name is required.");
        if (!ValueValidation.IsValidEmail(dto.Email))
            return Result<Guid>.Failure(ErrorCodes.Validation, "A valid email is required.");
        if (string.IsNullOrWhiteSpace(dto.Message))
            return Result<Guid>.Failure(ErrorCodes.Validation, "Message is required.");

        var entity = new Enquiry
        {
            FullName = dto.FullName.Trim(),
            Email = dto.Email.Trim(),
            Phone = dto.Phone,
            Subject = dto.Subject,
            Message = dto.Message,
            InterestType = dto.Interest,
            SourcePage = dto.SourcePage ?? "contact",
            Status = "New"
        };

        await _enquiries.AddAsync(entity, ct);
        await _enquiries.SaveChangesAsync(ct);

        await SafeSendAsync(() => _email.SendEnquiryEmailsAsync(entity, ct), "enquiry", entity.Id);
        return Result<Guid>.Success(entity.Id, "Your message has been received.");
    }

    public async Task<Result<Guid>> SubmitCampaignRequestAsync(CampaignRequestFormDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return Result<Guid>.Failure(ErrorCodes.Validation, "Full name is required.");
        if (!ValueValidation.IsValidEmail(dto.Email))
            return Result<Guid>.Failure(ErrorCodes.Validation, "A valid email is required.");

        var entity = new CampaignRequest
        {
            FullName = dto.FullName.Trim(),
            BrandName = dto.BrandName,
            Email = dto.Email.Trim(),
            Phone = dto.Phone,
            WhatToPromote = dto.PromoteWhat,
            CampaignType = dto.CampaignType,
            TargetAudience = dto.TargetAudience,
            Platforms = dto.Platforms ?? new List<string>(),
            BudgetRange = dto.Budget,
            CampaignGoal = dto.Goal,
            Message = dto.Message,
            Status = "New"
        };

        await _campaignRequests.AddAsync(entity, ct);
        await _campaignRequests.SaveChangesAsync(ct);

        await SafeSendAsync(() => _email.SendCampaignRequestEmailsAsync(entity, ct), "campaign-request", entity.Id);
        return Result<Guid>.Success(entity.Id, "Your campaign request has been received.");
    }

    public async Task<Result<Guid>> SubmitCreatorApplicationAsync(CreatorApplicationFormDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return Result<Guid>.Failure(ErrorCodes.Validation, "Full name is required.");
        if (!ValueValidation.IsValidEmail(dto.Email))
            return Result<Guid>.Failure(ErrorCodes.Validation, "A valid email is required.");

        var entity = new CreatorApplication
        {
            FullName = dto.FullName.Trim(),
            Email = dto.Email.Trim(),
            Phone = dto.Phone,
            City = dto.City,
            Province = dto.Province,
            MainPlatform = dto.Platform,
            HandleOrProfileLink = dto.Handle,
            FollowerRange = dto.Followers,
            ContentNiche = dto.Niche,
            ApplicationReason = dto.Why,
            Status = "Applied"
        };

        await _creatorApplications.AddAsync(entity, ct);
        await _creatorApplications.SaveChangesAsync(ct);

        await SafeSendAsync(() => _email.SendCreatorApplicationEmailsAsync(entity, ct), "creator-application", entity.Id);
        return Result<Guid>.Success(entity.Id, "Your creator application has been received.");
    }

    public async Task<Result<Guid>> SubscribeNewsletterAsync(NewsletterSubscribeDto dto, CancellationToken ct = default)
    {
        if (!ValueValidation.IsValidEmail(dto.Email))
            return Result<Guid>.Failure(ErrorCodes.Validation, "A valid email is required.");

        var email = dto.Email.Trim();
        var existing = await _subscribers.FirstOrDefaultAsync(s => s.Email == email, ct);
        if (existing is not null)
        {
            // Re-activate rather than create a duplicate active subscription.
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.UnsubscribedAt = null;
                existing.SubscribedAt = DateTime.UtcNow;
                _subscribers.Update(existing);
                await _subscribers.SaveChangesAsync(ct);
            }
            return Result<Guid>.Success(existing.Id, "You're subscribed.");
        }

        var entity = new NewsletterSubscriber
        {
            Email = email,
            FullName = dto.FullName,
            SourcePage = dto.SourcePage,
            IsActive = true,
            SubscribedAt = DateTime.UtcNow
        };
        await _subscribers.AddAsync(entity, ct);
        await _subscribers.SaveChangesAsync(ct);

        await SafeSendAsync(() => _email.SendNewsletterWelcomeAsync(entity, ct), "newsletter", entity.Id);
        return Result<Guid>.Success(entity.Id, "You're subscribed.");
    }

    public async Task<Result> UnsubscribeNewsletterAsync(string email, CancellationToken ct = default)
    {
        if (!ValueValidation.IsValidEmail(email))
            return Result.Failure(ErrorCodes.Validation, "A valid email is required.");

        var trimmed = email.Trim();
        var existing = await _subscribers.FirstOrDefaultAsync(s => s.Email == trimmed, ct);
        if (existing is { IsActive: true })
        {
            existing.IsActive = false;
            existing.UnsubscribedAt = DateTime.UtcNow;
            _subscribers.Update(existing);
            await _subscribers.SaveChangesAsync(ct);
        }
        // Enumeration-safe: same response whether or not the address existed.
        return Result.Success("You've been unsubscribed.");
    }

    private async Task SafeSendAsync(Func<Task<Result>> send, string kind, Guid id)
    {
        try
        {
            var result = await send();
            if (!result.IsSuccess)
                _logger.LogWarning("Notification emails for {Kind} {Id} reported: {Message}", kind, id, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification emails for {Kind} {Id} threw.", kind, id);
        }
    }
}
