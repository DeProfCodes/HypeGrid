# HypeGrid Backend API

Production-foundation .NET 8 Clean Architecture backend that powers the HypeGrid
public website (`HypeGridWebsite`) and admin dashboard (`HypeGridAdmin`). It
replaces the Base44 SDK both apps were prototyped on: it serves the content that
is currently hardcoded in the website, persists the three website forms that
currently only set local React state, and backs every admin entity (campaigns,
clients, creators, deliverables, quotes, invoices, payouts, etc.).

Built to match the conventions of the sibling `ZansiHustle` backend: ASP.NET
Core Identity + JWT/refresh tokens, EF Core (SQL Server), the
`Result`/`ErrorCodes` envelope, a `BaseController` that maps error codes to HTTP
status, a config-driven SMTP sender pool, and pure-C# email templates.

## Solution layout

```
HypeGrid/
  HypeGrid.sln
  HypeGrid.API             ASP.NET Core Web API — controllers, DI, middleware, JSON, appsettings
  HypeGrid.Application     Interfaces, DTOs, auth + lead + email services, dashboard contract
  HypeGrid.Domain          Entities + BaseEntity (Id / CreatedDate / UpdatedDate)
  HypeGrid.Infrastructure  EF DbContext + configs, generic Repository, JWT generator, SMTP provider, seeder, migrations
  HypeGrid.Shared          Result/ErrorCodes, AccountStatus enum, role + allowed-value constants
```

Project references: API → Application + Infrastructure; Application → Domain;
Infrastructure → Application + Domain + Shared; Domain → Shared.

## Stack

- .NET 8, ASP.NET Core Web API (controllers)
- EF Core 8, **SQL Server** provider
- ASP.NET Core Identity (`User : IdentityUser<Guid>`) + custom JWT & refresh tokens
- Swagger / OpenAPI
- SMTP email (System.Net.Mail) with a multi-identity sender pool
- **snake_case JSON** so the existing Base44-built frontends bind without renaming fields

## Getting started

### 1. Prerequisites
- .NET 8 SDK
- SQL Server or LocalDB (the dev connection string targets `(localdb)\MSSQLLocalDB`)
- EF tools: `dotnet tool install --global dotnet-ef`

### 2. Configure secrets (local)
The committed `appsettings.json` contains **no secrets**. For local dev, set a
JWT key + (optionally) SMTP credentials. Easiest is `appsettings.Development.json`
(already present with a dev JWT key + LocalDB connection), or user-secrets:

```bash
cd HypeGrid.API
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:Key" "<a-long-random-secret>"
# SMTP host/port/SSL/from/username live in appsettings.json — only the password is a secret:
dotnet user-secrets set "EmailProviders:Senders:Default:Password" "<noreply@ smtp-pass>"
dotnet user-secrets set "EmailProviders:Senders:Support:Password" "<support@ smtp-pass>"
```

Lead notifications go **To** `HypeGridEmail__AdminNotificationEmail`
(`support@hypegrid.co.za`). To also receive a private copy, set a **BCC** list
(comma/semicolon-separated; internal notifications only, never customer emails) —
prefer an env var / app-pool setting so it changes without a redeploy:

```
HypeGridEmail__AdminNotificationBccEmails=nproficientm@gmail.com
```

See [docs/CONFIGURATION.md](docs/CONFIGURATION.md#copying-a-personal-inbox-on-lead-notifications-adminnotificationbccemails).

Admin image uploads (hero/deals/featured-video) go to **Cloudflare R2** via
`POST /api/admin/assets/upload`; the DB stores only the public URL. Set the
`AssetStorage__*` env vars (R2 keys are secrets) — for dev without R2 use
`AssetStorage__Provider=Local`. Full setup (bucket `hypegrid-assets`, custom
domain `assets.hypegrid.co.za`, limits, recommended dimensions) is in
[docs/CONFIGURATION.md](docs/CONFIGURATION.md#asset-storage--images-cloudflare-r2).

### 3. Create the database
Migrations apply automatically on startup, or run manually:

```bash
dotnet ef database update --project HypeGrid.Infrastructure --startup-project HypeGrid.API
```

### 4. Run
```bash
dotnet run --project HypeGrid.API
```
Swagger UI: `http://localhost:<port>/swagger`. On first run (Development) the
seeder creates roles, a **dev SuperAdmin** (`admin@hypegrid.co.za` /
`ChangeMe123!` — change immediately), the 8 services + 5 packages lifted from the
website, and default site settings.

## Response envelope

Every endpoint returns:
```jsonc
// success
{ "success": true, "message": "…", "data": { /* … */ } }
// failure (status code derived from the error code)
{ "success": false, "code": "VALIDATION_ERROR", "message": "…" }
```
Entities serialize in **snake_case** with `id`, `created_date`, `updated_date`
(matching the Base44 field names the frontend already uses).

## Auth & roles

- `POST /api/auth/login` → `{ access_token, refresh_token, expires_at_utc, user }`
- `POST /api/auth/refresh`, `POST /api/auth/logout`, `GET /api/auth/me`
- `POST /api/auth/forgot-password`, `POST /api/auth/reset-password`
- Roles: `SuperAdmin, Admin, CampaignManager, Finance, Support, Client, Creator`.
  Admin endpoints require one of the admin roles (`RequireAdminAccess`); user
  management requires `SuperAdmin`. Public + content GETs are anonymous.

## Endpoint groups

- `GET  /api/public/*` — services, packages, testimonials, case-studies, site-settings (anonymous content)
- `POST /api/public/contact | campaign-requests | creator-applications | newsletter/subscribe|unsubscribe`
- `GET  /api/admin/dashboard/*` — summary + widgets + recent-activity tables
- `… /api/admin/{clients|creators|campaigns|deliverables|tasks|notes|enquiries|campaign-requests|creator-applications|quotes|invoices|payouts}` — CRUD + workflow actions
- `… /api/admin/content/{services|packages|testimonials|case-studies}` — CMS
- `… /api/admin/newsletter/subscribers`, `/api/admin/settings/{group}`, `/api/admin/users`

See `docs/FRONTEND_INTEGRATION.md` for the per-page mapping of frontend calls to
these endpoints, and `docs/CONFIGURATION.md` for the full appsettings/env-var guide.

## Open decisions (safe defaults applied; TODOs in code)

These were left as safe, reversible defaults — confirm before go-live:

1. Final production API base URL, website URL, admin URL.
2. ~~Final sender addresses + SMTP host/port.~~ **Resolved:** SmarterASP
   `mail.hypegrid.co.za:8889` (SSL off) is set in `appsettings.json` for all
   senders. Phase 1 mailboxes: `noreply@` (Default/NoReply) + `support@`
   (Support); Campaigns/Creators fall back to Default until created. Only the
   `Default` and `Support` SMTP passwords are required (env vars).
3. CMS scope — services/packages are seeded + CMS-managed; the rest of the
   homepage copy (hero, "what we promote", pillars) is still static in the site.
4. Packages are **quote-based** (`price_label = "Request Quote"`); no numeric prices.
5. File uploads (logos/posters/song art) are URL placeholders for now (no blob storage).
6. Clients/Creators are **admin-managed only** in phase 1; the `Client`/`Creator`
   roles exist for a future self-service portal.
7. Payments are **manual tracking** (quotes/invoices/payouts) — no gateway integration.
8. No SMS/WhatsApp yet (the email subsystem mirrors ZansiHustle so those can be added).
