using Microsoft.Extensions.Options;
using HypeGrid.Application.Communication.Email;
using HypeGrid.Application.Communication.Email.Interfaces;
using HypeGrid.Application.Communication.Email.Models;
using HypeGrid.Infrastructure.Configuration;

namespace HypeGrid.Infrastructure.Communications.Email;

/// <summary>
/// Resolves the SMTP options for a logical <see cref="EmailSender"/> from the
/// configured sender pool. Falls back to the "Default" slot when a specific
/// identity is not configured, so a partial config still sends mail.
/// </summary>
public sealed class EmailSenderMapper : IEmailSenderMapper
{
    private readonly EmailSenderSettings _settings;

    public EmailSenderMapper(IOptions<EmailSenderSettings> settings) => _settings = settings.Value;

    public EmailSenderOptions Resolve(EmailSender sender)
    {
        var name = sender.ToString();
        if (_settings.Senders.TryGetValue(name, out var options) && options.IsConfigured)
            return options;

        if (_settings.Senders.TryGetValue(nameof(EmailSender.Default), out var fallback))
            return fallback;

        return new EmailSenderOptions();
    }
}
