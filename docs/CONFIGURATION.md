# HypeGrid — Configuration & Environment Guide

All settings live in `HypeGrid.API/appsettings.json` (committed, **no secrets**)
and can be overridden per environment with environment variables using the
double-underscore convention (`Section__Key`) or `dotnet user-secrets` in dev.

## Connection string

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection. Dev override targets LocalDB. |

```
ConnectionStrings__DefaultConnection="Server=…;Database=HypeGridDb;User Id=…;Password=…;Encrypt=True;TrustServerCertificate=True;"
```

## JWT (`JwtSettings`)

| Key | Default | Notes |
|---|---|---|
| `JwtSettings__Key` | _(empty — must set)_ | Long random signing secret. **Required** — app refuses to start without it. |
| `JwtSettings__Issuer` | `HypeGrid` | |
| `JwtSettings__Audience` | `HypeGrid.Clients` | |
| `JwtSettings__AccessTokenExpirationMinutes` | `60` | |
| `JwtSettings__RefreshTokenExpirationDays` | `30` | |

## Dev admin seed (`Seed`)

Development convenience only — **not** the production path (use `BootstrapAdmin`).

| Key | Default | Notes |
|---|---|---|
| `Seed__ForceAdmin` | `false` | Set `true` to seed the admin outside Development. Leave `false` in prod. |
| `Seed__AdminEmail` | `admin@hypegrid.co.za` | |
| `Seed__AdminPassword` | `ChangeMe123!` | **Change immediately.** Dev-only convenience. |

## Production bootstrap admin (`BootstrapAdmin`)

The safe way to create the **first** live SuperAdmin with no raw SQL. The startup
seeder calls `UserManager.CreateAsync(user, password)` so the password hash,
security stamp, normalized email/username, and lockout fields are all set
correctly. OFF by default; everything comes from env / app-pool settings — **never
put the password in `appsettings.json` or source.** The password is never logged.

| Key | Default | Notes |
|---|---|---|
| `BootstrapAdmin__Enabled` | `false` | Master switch. Set `true` for the first boot, then back to `false`. |
| `BootstrapAdmin__Email` | _(none)_ | Required when enabled. |
| `BootstrapAdmin__Password` | _(none)_ | Required when enabled. **Env var only.** Never committed/logged. |
| `BootstrapAdmin__PhoneNumber` | _(none)_ | Optional. If set, `PhoneNumberConfirmed` is marked true. |
| `BootstrapAdmin__Role` | `SuperAdmin` | Role to assign (created if it doesn't exist). |
| `BootstrapAdmin__ResetPassword` | `false` | If the user already exists, set `true` once to reset its password to the configured value. |

Behavior:
- **Enabled + user doesn't exist** → creates it (`EmailConfirmed=true`, phone set,
  `Active`), assigns the role, logs a warning to disable the switch.
- **Enabled + user exists** → ensures the role is assigned; **does not** reset the
  password unless `BootstrapAdmin__ResetPassword=true`.
- **Disabled** → does nothing.

> ⚠️ **After the first successful login, set `BootstrapAdmin__Enabled=false`**
> (and `BootstrapAdmin__ResetPassword=false` if you used it) and restart, then
> change the password in the portal / via a secure reset flow.

## Email branding/routing (`HypeGridEmail`)

| Key | Default |
|---|---|
| `HypeGridEmail__AdminNotificationEmail` | `support@hypegrid.co.za` — primary **To** for all new-lead notifications |
| `HypeGridEmail__AdminNotificationBccEmails` | _(empty)_ — extra inboxes **BCC'd on internal lead notifications only** |
| `HypeGridEmail__CompanyName` / `CompanyLegalName` | `HypeGrid` |
| `HypeGridEmail__PrimaryColor` | `#06b6d4` |
| `HypeGridEmail__WebsiteUrl` | `https://hypegrid.co.za` |
| `HypeGridEmail__AdminBaseUrl` | `https://admin.hypegrid.co.za` (also used for the password-reset link host) |

All admin/internal notifications go to the single `AdminNotificationEmail` inbox
(as **To**) and are sent from the `Default` identity. These values are **not
secrets** and stay in `appsettings.json`.

### Copying a personal inbox on lead notifications (`AdminNotificationBccEmails`)

To also receive a private copy of every internal lead notification, set a **BCC**
list. It is comma/semicolon-separated, so one or several addresses work:

```
HypeGridEmail__AdminNotificationBccEmails=nproficientm@gmail.com
# multiple:
HypeGridEmail__AdminNotificationBccEmails=owner@hypegrid.co.za;nproficientm@gmail.com
```

Final behaviour for a lead notification:

```
To:  support@hypegrid.co.za
Bcc: nproficientm@gmail.com
```

Scope — BCC is applied **only** to the internal admin notifications for:
Contact/Enquiry, Campaign Request, and Creator Application (and any future flow
that uses the same admin-notification path). It is **never** added to customer
acknowledgements, the newsletter/alerts welcome, or password-reset emails, and
BCC addresses are not exposed to the primary recipient. Prefer setting this as an
env var / app-pool setting so it can change without a redeploy; the
`appsettings.json` default is intentionally empty.

## SMTP sender pool (`EmailProviders:Senders:<Name>`)

Five logical senders: `Default`, `Support`, `Campaigns`, `Creators`, `NoReply`.
Each has the same shape. **All SMTP config except the password lives in
`appsettings.json`. Only `Password` is a secret — supply it via env var /
app-pool setting; never commit it.**

| Key (per sender) | In appsettings? | Secret / env var? |
|---|---|---|
| `EmailProviders__Senders__<Name>__Host` | yes (`mail.hypegrid.co.za`) | no |
| `EmailProviders__Senders__<Name>__Port` | yes (`8889`) | no |
| `EmailProviders__Senders__<Name>__Username` | yes (full email address) | no |
| `EmailProviders__Senders__<Name>__FromEmail` | yes | no |
| `EmailProviders__Senders__<Name>__FromName` | yes | no |
| `EmailProviders__Senders__<Name>__EnableSsl` | yes (`false`) | no |
| `EmailProviders__Senders__<Name>__Password` | **left empty** | **YES — env var only** |

A sender is considered *configured* when its `Host` **and** `FromEmail` are
non-empty (Username/Port/EnableSsl/Password are **not** part of that check). An
**unconfigured** sender (empty `Host`) falls back to `Default` — same mailbox,
so it needs no separate password.

The boot-time reporter logs one of three states per sender (names only, never
secrets):

- `ready (host:port, ssl=…)` — configured **and** a password is present.
- `configured … but PASSWORD missing` — Host/FromEmail set but no password yet;
  sends will fail until the password env var is supplied.
- `not configured … falls back to 'Default'` — empty Host; routed to `Default`.

Lead capture never fails on a mail outage (the record is saved first; email is
best-effort and failures are logged).

### Phase-1 production setup (SmarterASP)

SMTP host/port/SSL are filled into `appsettings.json` for every sender:

```
Host: mail.hypegrid.co.za
Port: 8889
EnableSsl: false
```

Mailboxes to create first: **`noreply@hypegrid.co.za`** and
**`support@hypegrid.co.za`**. `Default` + `NoReply` use `noreply@`; `Support`
uses `support@`. `NoReply`, `Campaigns`, and `Creators` are left **unconfigured**
(empty Host) so they fall back to `Default` (`noreply@`) — no extra mailbox or
password needed for them yet.

**Only two email passwords are required for phase 1** (env var / app-pool only):

```
EmailProviders__Senders__Default__Password   = <noreply@ mailbox password>
EmailProviders__Senders__Support__Password   = <support@ mailbox password>
```

`EmailProviders__Senders__NoReply__Password` is **NOT required** — NoReply falls
back to Default (the same `noreply@` mailbox, identical From). Likewise
`Campaigns`/`Creators` passwords are not required until you create those
mailboxes and set their `Host`.

## Asset storage — images (Cloudflare R2)

Admin image uploads (hero/deals/featured-video/brand/campaign/creator) are stored
in **Cloudflare R2** (S3-compatible). **Binaries are never stored in SQL** — the
database only keeps the resulting public URL in the existing `*_image_url` fields.
Endpoint: `POST /api/admin/assets/upload` (admin-only, `multipart/form-data` with
`file` + `category`).

### Config (`AssetStorage`)

| Key | Default | Notes |
|---|---|---|
| `AssetStorage__Provider` | `R2` | `R2` (Cloudflare) or `Local` (dev disk). |
| `AssetStorage__PublicBaseUrl` | `https://assets.hypegrid.co.za` | Public asset host (your R2 custom domain). |
| `AssetStorage__BucketName` | `hypegrid-assets` | R2 bucket name. |
| `AssetStorage__R2__AccountId` | _(empty)_ | Cloudflare account id (used to derive the endpoint). |
| `AssetStorage__R2__Endpoint` | _(empty)_ | Override; defaults to `https://<AccountId>.r2.cloudflarestorage.com`. |
| `AssetStorage__R2__AccessKeyId` | _(empty)_ | **Secret** — env/app-pool only. |
| `AssetStorage__R2__SecretAccessKey` | _(empty)_ | **Secret** — env/app-pool only. |

Production env vars:

```
AssetStorage__Provider=R2
AssetStorage__PublicBaseUrl=https://assets.hypegrid.co.za
AssetStorage__BucketName=hypegrid-assets
AssetStorage__R2__AccountId=<cloudflare-account-id>
AssetStorage__R2__Endpoint=https://<account-id>.r2.cloudflarestorage.com
AssetStorage__R2__AccessKeyId=<r2-access-key-id>
AssetStorage__R2__SecretAccessKey=<r2-secret-access-key>
```

If R2 isn't configured the **API still starts** — the upload endpoint returns a
clear `422 PROVIDER_NOT_CONFIGURED` instead of crashing.

### Categories, limits & recommended dimensions

| `category` | R2 key prefix | Max size | Recommended |
|---|---|---|---|
| `hero-desktop` | `hero/desktop/` | 8 MB | 1920×1080 |
| `hero-mobile` | `hero/mobile/` | 8 MB | 1080×1920 |
| `deal` | `deals/` | 5 MB | 1200×800 or 1080×1080 |
| `featured-video` | `featured-video/` | 5 MB | 1280×720 (thumbnail) |
| `brand` | `brand/` | 5 MB | 1080×1080 |
| `campaign` | `campaigns/` | 5 MB | 1200×800 |
| `creator` | `creators/` | 5 MB | 1080×1080 |

Allowed types: **JPEG, PNG, WEBP** (validated by magic-bytes, not the client
extension). Object keys are server-generated: `<prefix>yyyy/MM/<safe-name>-<id>.<ext>`.
Dimensions are **not** enforced in phase 1 — the recommendations are returned in
the upload response and shown in the admin form.

### Cloudflare dashboard setup (one-time)

1. Create an R2 bucket named **`hypegrid-assets`**.
2. Create an **R2 API token** (Object Read & Write) → gives Access Key ID + Secret.
3. Connect a **custom domain** `assets.hypegrid.co.za` to the bucket (R2 → Settings
   → Public access → Custom Domains) so objects are publicly served over your domain.
4. Set the env vars above on the API host.
5. **CORS:** uploads are server-side (S3 PutObject), so no bucket CORS is needed
   for uploading. Public **GET** of images from the website/admin works via the
   custom domain with no CORS config (plain `<img>` loads). Only add R2 bucket CORS
   if you later fetch assets via browser `fetch()`/XHR.

### Local dev mode (no R2)

Set `AssetStorage__Provider=Local` and `AssetStorage__PublicBaseUrl=http://localhost:5247`
(the API origin). Files are written to `wwwroot/uploads/<key>` and served by the
API's static files — handy for testing the admin upload flow without R2.

## CORS

> ⚠️ **Current state: TEMPORARILY PERMISSIVE.** To unblock Vercel/website/admin
> deployment testing, the non-Development branch in
> `ServiceExtensions.AddCustomCors` currently uses
> `AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()` (no credentials — safe
> because both frontends are Bearer-token only, no cookies). `HYPEGRID_CORS_ORIGINS`
> is **not required** while permissive. The previous env-driven allow-list is kept
> as a commented "RESTORE FOR PRODUCTION" block right below it.

**Before go-live**, lock CORS back down to the final origins
(`https://hypegrid.co.za`, `https://www.hypegrid.co.za`,
`https://portal.hypegrid.co.za`, `https://admin.hypegrid.co.za`) by restoring that
block. Full detail in [`CORS_AND_DEPLOYMENT.md`](./CORS_AND_DEPLOYMENT.md).

## Database migrations

Migrations **auto-apply on startup in every environment** — `Program.cs` calls
`MigrateAndSeedAsync()`, which runs `db.Database.MigrateAsync()` (fatal on
failure: the host refuses to start) and then the idempotent seeder. No env var
gates this. To apply migrations manually instead (e.g. against a locked-down DB
before the app starts):

```
dotnet ef database update --project HypeGrid.Infrastructure --startup-project HypeGrid.API
```

## Example `.env` for the frontends

```
# HypeGridWebsite/.env  and  HypeGridAdmin/.env
VITE_API_BASE_URL=http://localhost:5249
```

## Production checklist

- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`.
- [ ] Set `JwtSettings__Key` to a strong secret (env var / secret store).
- [ ] Set the real `ConnectionStrings__DefaultConnection`.
- [ ] Create the `noreply@` and `support@` mailboxes on SmarterASP.
- [ ] Set **only** the two SMTP passwords as env vars / app-pool settings:
      `EmailProviders__Senders__Default__Password` and
      `EmailProviders__Senders__Support__Password`. (Host/Port/SSL/From/Username
      are already in `appsettings.json` — do not add them as env vars.)
- [ ] Confirm `HypeGridEmail__AdminNotificationEmail = support@hypegrid.co.za`.
- [ ] **First admin:** set `BootstrapAdmin__Enabled=true` + `Email`/`Password`/
      `PhoneNumber` for the first boot; log in once; then set
      `BootstrapAdmin__Enabled=false` and restart, and change the password in
      the portal. Keep `Seed__ForceAdmin=false`.
- [ ] Lock CORS back down to final origins (currently temporarily permissive).
- [ ] Re-enable `RequireHttpsMetadata` (set in `AddIdentityServices`) behind TLS.
- [ ] Migrations auto-apply on startup; or run `dotnet ef database update` first.

## Production SQL / first-boot checklist

A blank database is fine — startup brings it fully up:

1. Empty DB created (just an empty schema; connection string points at it).
2. API starts (`ASPNETCORE_ENVIRONMENT=Production`).
3. Migrations applied automatically (`MigrateAndSeedAsync`).
4. Identity tables exist — named `Users`, `Roles`, `UserRoles`, `UserClaims`,
   `RoleClaims`, `UserLogins`, `UserTokens` (plus `RefreshTokens`). These are the
   renamed ASP.NET Identity tables (not the default `AspNet*` names).
5. Roles exist: `SuperAdmin`, `Admin`, `CampaignManager`, `Finance`, `Support`,
   `Client`, `Creator` (seeded every boot, idempotent).
6. Bootstrap SuperAdmin exists (if `BootstrapAdmin__Enabled=true`).
7. Seeded public `Services` + `Packages` (and default `SiteSettings`) exist.
8. Admin login works via `POST /api/auth/login`.
```
