using HypeGrid.Application.Communication.Email.Models;
using HypeGrid.Shared.Results;

namespace HypeGrid.Application.Communication.Email.Interfaces;

/// <summary>
/// Low-level, transport-agnostic email send abstraction. The SMTP
/// implementation lives in Infrastructure. Returns a <see cref="Result"/> — it
/// never throws on a send failure.
/// </summary>
public interface IEmailProvider
{
    Task<Result> SendAsync(EmailMessage message, CancellationToken ct = default);
}

/// <summary>Resolves the SMTP options for a given logical sender identity.</summary>
public interface IEmailSenderMapper
{
    EmailSenderOptions Resolve(EmailSender sender);
}
