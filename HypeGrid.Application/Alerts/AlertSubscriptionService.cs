using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using HypeGrid.Application.Alerts.Dtos;
using HypeGrid.Application.Common.Interfaces;
using HypeGrid.Application.Common.Validation;
using HypeGrid.Application.Communication.Email.Interfaces;
using HypeGrid.Domain.Alerts;
using HypeGrid.Domain.Content;
using HypeGrid.Shared.Errors;
using HypeGrid.Shared.Results;

namespace HypeGrid.Application.Alerts;

/// <summary>
/// Default HypeGrid Alerts / Deals Club opt-in service. Persist-first; the
/// acknowledgement email is best-effort. Consent is required and stored with its
/// version + verbatim snapshot. We store only <c>ChannelJoinClicked</c>, never a
/// (verifiable-impossible) channel join, and never send WhatsApp messages here.
/// </summary>
public sealed class AlertSubscriptionService : IAlertSubscriptionService
{
    /// <summary>Site-settings key (group "communication") holding the public WhatsApp Channel URL.</summary>
    public const string ChannelUrlSettingKey = "whatsapp_channel_url";

    /// <summary>Live HypeGrid WhatsApp Channel — fallback when the SiteSetting is missing.</summary>
    public const string ChannelUrlFallback = "https://whatsapp.com/channel/0029VbCla7vEQIaqrNnT1E2e";

    public const string CurrentConsentVersion = "hypegrid-alerts-v1";

    /// <summary>Canonical v1 consent copy — used if the client doesn't send a snapshot.</summary>
    public const string DefaultConsentSnapshot =
        "I agree that HypeGrid may store the details I've provided and send me relevant deals, " +
        "specials, campaigns, events, and promotions. I understand I can opt out at any time. " +
        "See HypeGrid's Privacy Policy.";

    private const int MaxInterests = 25;

    private readonly IRepository<AlertSubscriber> _subscribers;
    private readonly IRepository<SiteSetting> _settings;
    private readonly IEmailService _email;
    private readonly ILogger<AlertSubscriptionService> _logger;

    public AlertSubscriptionService(
        IRepository<AlertSubscriber> subscribers,
        IRepository<SiteSetting> settings,
        IEmailService email,
        ILogger<AlertSubscriptionService> logger)
    {
        _subscribers = subscribers;
        _settings = settings;
        _email = email;
        _logger = logger;
    }

    public async Task<Result<AlertSubscribeResultDto>> SubscribeAsync(
        AlertSubscribeDto dto, string? userAgent, string? ipAddress, CancellationToken ct = default)
    {
        // Consent is mandatory and must be an active opt-in.
        if (!dto.Consent)
            return Result<AlertSubscribeResultDto>.Failure(ErrorCodes.Validation, "Consent is required to join HypeGrid Alerts.");

        var email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        var phone = NormalizePhone(dto.Phone);

        if (email is not null && !ValueValidation.IsValidEmail(email))
            return Result<AlertSubscribeResultDto>.Failure(ErrorCodes.Validation, "Please provide a valid email address.");

        if (email is null && phone is null)
            return Result<AlertSubscribeResultDto>.Failure(ErrorCodes.Validation, "Please provide a WhatsApp number or an email address.");

        var interests = (dto.Interests ?? new List<string>())
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => i.Trim())
            .Distinct()
            .Take(MaxInterests)
            .ToList();

        var version = string.IsNullOrWhiteSpace(dto.ConsentVersion) ? CurrentConsentVersion : dto.ConsentVersion.Trim();
        var snapshot = string.IsNullOrWhiteSpace(dto.ConsentSnapshot) ? DefaultConsentSnapshot : dto.ConsentSnapshot.Trim();
        var now = DateTime.UtcNow;

        // De-dupe by phone or email: update/reactivate rather than create duplicates.
        var existing = await FindExistingAsync(email, phone, ct);

        AlertSubscriber entity;
        if (existing is not null)
        {
            entity = existing;
            entity.FullName = dto.FullName?.Trim() ?? entity.FullName;
            if (email is not null) entity.Email = email;
            if (phone is not null) entity.PhoneNumber = phone;
            entity.City = dto.City?.Trim() ?? entity.City;
            entity.Province = dto.Province?.Trim() ?? entity.Province;
            if (interests.Count > 0) entity.Interests = interests;
            entity.FrequencyPreference = dto.FrequencyPreference?.Trim() ?? entity.FrequencyPreference;
            entity.SourcePage = dto.SourcePage?.Trim() ?? entity.SourcePage;
            // Re-consent refreshes the audit trail.
            entity.ConsentGiven = true;
            entity.ConsentTextVersion = version;
            entity.ConsentSnapshot = snapshot;
            entity.ConsentAt = now;
            entity.ConsentIpHash = HashIp(ipAddress);
            entity.ConsentUserAgent = Truncate(userAgent, 512);
            entity.IsActive = true;
            entity.Status = "Active";
            entity.UnsubscribedAt = null;
            _subscribers.Update(entity);
        }
        else
        {
            entity = new AlertSubscriber
            {
                FullName = dto.FullName?.Trim(),
                Email = email,
                PhoneNumber = phone,
                City = dto.City?.Trim(),
                Province = dto.Province?.Trim(),
                Interests = interests,
                FrequencyPreference = dto.FrequencyPreference?.Trim(),
                SourcePage = dto.SourcePage?.Trim(),
                Status = "Active",
                IsActive = true,
                ConsentGiven = true,
                ConsentTextVersion = version,
                ConsentSnapshot = snapshot,
                ConsentAt = now,
                ConsentIpHash = HashIp(ipAddress),
                ConsentUserAgent = Truncate(userAgent, 512),
                DirectMessageOptIn = false, // phase 1: never set here
            };
            await _subscribers.AddAsync(entity, ct);
        }

        await _subscribers.SaveChangesAsync(ct);

        // Best-effort acknowledgement — a mail outage must never fail the opt-in.
        if (!string.IsNullOrWhiteSpace(entity.Email))
            await SafeSendWelcomeAsync(entity, ct);

        var url = await GetChannelUrlAsync(ct);
        return Result<AlertSubscribeResultDto>.Success(
            new AlertSubscribeResultDto { Id = entity.Id, WhatsappChannelUrl = url },
            "You're on the HypeGrid Deals list.");
    }

    public async Task<Result<AlertChannelClickResultDto>> MarkChannelClickedAsync(Guid id, CancellationToken ct = default)
    {
        var url = await GetChannelUrlAsync(ct);
        var entity = await _subscribers.GetByIdAsync(id, ct);
        if (entity is null)
            return Result<AlertChannelClickResultDto>.Failure(ErrorCodes.NotFound, "Subscriber not found.");

        // Idempotent: only stamp the first click.
        if (!entity.ChannelJoinClicked)
        {
            entity.ChannelJoinClicked = true;
            entity.ChannelJoinClickedAt = DateTime.UtcNow;
            _subscribers.Update(entity);
            await _subscribers.SaveChangesAsync(ct);
        }

        return Result<AlertChannelClickResultDto>.Success(
            new AlertChannelClickResultDto { WhatsappChannelUrl = url }, "Recorded.");
    }

    public async Task<Result> UnsubscribeAsync(string? email, string? phone, CancellationToken ct = default)
    {
        var normEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        var normPhone = NormalizePhone(phone);

        if (normEmail is null && normPhone is null)
            return Result.Failure(ErrorCodes.Validation, "Provide an email or phone to opt out.");

        var existing = await FindExistingAsync(normEmail, normPhone, ct);
        if (existing is { IsActive: true })
        {
            existing.IsActive = false;
            existing.Status = "OptedOut";
            existing.UnsubscribedAt = DateTime.UtcNow;
            _subscribers.Update(existing);
            await _subscribers.SaveChangesAsync(ct);
        }

        // Enumeration-safe: identical response whether or not the contact existed.
        return Result.Success("You've been opted out of HypeGrid Alerts.");
    }

    public async Task<string> GetChannelUrlAsync(CancellationToken ct = default)
    {
        var row = await _settings.FirstOrDefaultAsync(s => s.Key == ChannelUrlSettingKey, ct);
        return string.IsNullOrWhiteSpace(row?.Value) ? ChannelUrlFallback : row!.Value!.Trim();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private Task<AlertSubscriber?> FindExistingAsync(string? email, string? phone, CancellationToken ct)
        => _subscribers.FirstOrDefaultAsync(
            s => (phone != null && s.PhoneNumber == phone) || (email != null && s.Email == email), ct);

    private async Task SafeSendWelcomeAsync(AlertSubscriber subscriber, CancellationToken ct)
    {
        try
        {
            var result = await _email.SendAlertWelcomeAsync(subscriber, ct);
            if (!result.IsSuccess)
                _logger.LogWarning("Alert welcome email for {Id} reported: {Message}", subscriber.Id, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert welcome email for {Id} threw.", subscriber.Id);
        }
    }

    /// <summary>
    /// Best-effort phone normalization toward E.164. South African local formats
    /// (0XXXXXXXXX) become +27XXXXXXXXX; "27…"/"0027…" are normalized; numbers
    /// already in "+…" form are kept. Anything we can't confidently map is returned
    /// cleaned (digits preserved) rather than rejected — we never break a valid
    /// international number.
    /// </summary>
    internal static string? NormalizePhone(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var hadPlus = input.TrimStart().StartsWith('+');
        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;

        if (hadPlus)
            return "+" + digits;

        if (digits.StartsWith("0027"))
            return "+27" + digits[4..];
        if (digits.StartsWith("27") && digits.Length == 11)
            return "+" + digits;
        if (digits.StartsWith("0") && digits.Length == 10)
            return "+27" + digits[1..];

        // Unknown shape — keep the digits, don't guess a country code.
        return digits;
    }

    private static string? HashIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return null;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(ip.Trim()));
        return Convert.ToHexString(bytes);
    }

    private static string? Truncate(string? value, int max)
        => string.IsNullOrEmpty(value) ? value : (value.Length <= max ? value : value[..max]);
}
