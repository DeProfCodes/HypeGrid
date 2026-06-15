# HypeGrid Alerts / WhatsApp Channel Opt-in — Implementation Spec

> **Status: PLANNING / SPEC ONLY.** Nothing in this document is implemented yet.
> No backend code, frontend code, migrations, or shared files have been changed.
> This is a reference for the future implementation and for the parallel session
> currently building hero carousel / deals / featured video / analytics.

## Scope guardrails (read first)

- **Phase 1 is questionnaire + database + WhatsApp Channel CTA only.** A public
  opt-in form, an `AlertSubscriber` table, an admin management page, and a button
  that links to the public WhatsApp **Channel**.
- **No Meta WhatsApp Business API sending yet.** We do not send WhatsApp messages,
  templates, or direct messages in phase 1.
- **We only store `ChannelJoinClicked`, never `JoinedChannel`.** We cannot verify
  that someone actually joined a WhatsApp Channel, so we never claim or store that
  they did — we record only that they tapped the CTA button.
- **Consent must be stored with version + snapshot.** Persist the exact consent
  text shown and a version tag, plus a timestamp (and a hashed IP for audit).
- **Direct WhatsApp message consent is separate and future-only.** Joining a public
  channel is NOT consent to be messaged 1:1. A distinct `DirectMessageOptIn` flag
  stays `false` until the Meta-API phase.
- **The WhatsApp Channel URL is a public, non-secret value.** It lives as a public
  site setting (admin-editable), never as an environment variable / secret.
- **Build this only after the hero/deals/analytics branch is merged.** See
  [§10](#10-implementation-order-after-the-herodealsanalytics-branch-merges) and
  the [merge-conflict hotspots](#merge-conflict-hotspots).

---

## Conventions this plan follows (from the current code)

- Entities derive from `BaseEntity` → `Guid Id`, `CreatedDate`, `UpdatedDate`,
  serialized **snake_case** (`id`, `created_date`). Timestamps are stamped
  centrally in `AppDbContext.SaveChanges`.
- Public conversion flow = **persist-first, side-effects best-effort, never fails
  the form** (see `HypeGrid.Application/Leads/PublicLeadService.cs`).
- Public endpoints are anonymous under `api/public`; admin endpoints are under
  `api/admin` with `[Authorize(Policy = HypeGridPolicies.RequireAdminAccess)]`.
- Responses use the `Result` envelope via `BaseController.Data(...)` /
  `ToActionResult(...)`.
- `List<string>` columns map to EF Core 8 JSON primitive collections (as
  `CampaignRequest.Platforms` does).
- Allowed-value sets live in `HypeGridValues`; analytics goes through the existing
  anonymous `AnalyticsEvent` pipeline (`POST /api/public/analytics/events`).

### Why this is low-risk to add

The infrastructure already anticipates the feature:

- `"whatsapp_click"` already exists in `HypeGridValues.AnalyticsEventTypes`.
- The public `GET /api/public/site-settings` endpoint already exposes the
  `communication` settings group — the ideal home for the channel URL.
- `NewsletterSubscriber` is a close analog (unique email, reactivate-on-duplicate,
  active/opt-out), so the new flow reuses well-trodden patterns.

---

## Merge-conflict hotspots

Do this work **after** the parallel hero/deals/analytics branch merges, then rebase
onto it before starting. These files/areas are the likely collision points:

| Area | Why it conflicts |
|---|---|
| `HypeGrid.API/Controllers/PublicController.cs` | New `alerts/*` endpoints touch the same shared controller. |
| `HypeGrid.Shared/Constants/HypeGridValues.cs` | Adding `alert_subscribe` / `AlertSubscription` to the analytics arrays. **Owned by the analytics session.** |
| `HypeGrid.Infrastructure/Data/AppDbContext.cs` | New `DbSet<AlertSubscriber>` + `OnModelCreating`. |
| EF **migrations + model snapshot** | Generate the new migration *after* the merge to avoid snapshot conflicts. |
| `HypeGrid.API/Extensions/ServiceExtensions.cs` | DI registration of the new service. |
| Admin routing + sidebar nav | New "Alerts" page entry. |
| Website routing + nav | New `/alerts` page entry. |

---

## 1. Recommended entity / table design

New entity `AlertSubscriber : BaseEntity` (suggested namespace
`HypeGrid.Domain.Alerts`; `HypeGrid.Domain.Leads` alongside `NewsletterSubscriber`
is also acceptable). Table `AlertSubscribers`.

| Property | Type | Notes |
|---|---|---|
| `FullName` | `string?` | optional, max 200 |
| `Email` | `string?` | optional, max 256 |
| `PhoneNumber` | `string?` | optional, max 30, normalize to E.164 (`+27…`) for future WhatsApp API |
| `City` / `Province` | `string?` | optional, reuse SA geography lists used by `CreatorApplication` |
| `Interests` | `List<string>` | deal/campaign categories (airtime-data, groceries, fashion, tech, food, events, campaigns…) — JSON column |
| `SourcePage` | `string?` | max 100, e.g. `alerts`, `home`, `deals` |
| `Status` | `string` | `New` / `Active` / `OptedOut`, max 50, required (mirrors other leads) |
| `IsActive` | `bool` | default `true`; opt-out sets `false` |
| `ConsentGiven` | `bool` | **must be true to persist** |
| `ConsentTextVersion` | `string?` | version tag of the exact consent copy shown (audit) |
| `ConsentSnapshot` | `string?` | the literal consent text the user agreed to (POPIA proof-of-consent) |
| `ConsentAt` | `DateTime` | when consent was captured |
| `ConsentIpHash` | `string?` | one-way IP hash (reuse the `AnalyticsEvent.IpHash` approach — never raw IP) |
| `ConsentUserAgent` | `string?` | optional audit |
| `ChannelJoinClicked` | `bool` | default `false` — **only set true when they click the CTA** |
| `ChannelJoinClickedAt` | `DateTime?` | timestamp of first click |
| `DirectMessageOptIn` | `bool` | default `false` — **separate, future-only** consent to receive 1:1 WhatsApp messages via Meta API. Distinct from "joined a public channel". |
| `UnsubscribedAt` | `DateTime?` | set on opt-out |

**Key design points**

- We **never store "JoinedChannel"** — only `ChannelJoinClicked`. We cannot verify
  a WhatsApp Channel join, so we do not claim it.
- **Channel membership consent ≠ direct-messaging consent.** `DirectMessageOptIn`
  is captured separately and stays `false` until the Meta-API phase ([§9](#9-future-meta-whatsapp-business-api-upgrade-path)).
- **De-dupe** like `NewsletterSubscriber`: filtered-unique index on `Email`; index
  on `PhoneNumber`. A repeat submit reactivates/updates rather than inserting a
  duplicate active row.
- Indexes: `CreatedDate`, `Status`, `ChannelJoinClicked` (admin filtering);
  unique-filtered `Email`.
- Require **at least one** of `Email` / `PhoneNumber` at the service layer.

EF config goes in a new `AlertsConfigurations.cs` under
`Infrastructure/Data/Configurations` (same shape as `LeadsConfigurations.cs`), plus
a `DbSet<AlertSubscriber>` on `AppDbContext` and **one new migration** (generated
after the upstream merge).

---

## 2. Public API endpoints (anonymous, `api/public`)

All persist-first with best-effort side-effects, returning the `Result` envelope.
Add to a new `IAlertSubscriptionService` (mirrors `IPublicLeadService`) rather than
overloading the lead service.

| Method / route | Body | Behavior |
|---|---|---|
| `POST /api/public/alerts/subscribe` | `AlertSubscribeDto` | Validate (≥1 contact, `consent==true`); persist `AlertSubscriber` (`Status=New`→`Active`); de-dupe by email/phone; best-effort acknowledgement email via the existing `NoReply`→Default sender. **Returns `{ id, whatsapp_channel_url }`** so the UI can render the CTA. |
| `POST /api/public/alerts/{id}/channel-click` | _(none)_ | Idempotently sets `ChannelJoinClicked=true` + `ChannelJoinClickedAt` (first time only). Also emits a `whatsapp_click` analytics event. Returns `{ whatsapp_channel_url }`. Forgiving — never blocks the redirect. |
| `POST /api/public/alerts/unsubscribe` | `{ email?, phone? }` | Enumeration-safe opt-out (same response whether or not found); sets `IsActive=false`, `Status=OptedOut`, `UnsubscribedAt` — exactly like `UnsubscribeNewsletterAsync`. |

`AlertSubscribeDto` (snake_case in/out): `full_name?`, `email?`, `phone?`, `city?`,
`province?`, `interests: string[]`, `consent: bool`, `consent_version?`,
`source_page?`.

> The WhatsApp URL is **not** a secret and is returned to the client deliberately;
> it is also available via `GET /api/public/site-settings` ([§7](#7-environment--config-for-the-whatsapp-channel-url)), so the UI
> can render the CTA even before submit if desired.

---

## 3. Admin API endpoints (`api/admin`, RequireAdminAccess)

New `AlertsAdminController` modeled on `NewsletterAdminController`.

| Method / route | Purpose |
|---|---|
| `GET /api/admin/alerts/subscribers?sort=-created_date&limit=200&status=&clicked=` | List with sort/limit + optional status / clicked filters |
| `GET /api/admin/alerts/subscribers/{id}` | Detail (incl. consent snapshot + timestamp for audit) |
| `PATCH /api/admin/alerts/subscribers/{id}/status` | `{ status: "Active" \| "OptedOut" }` — toggles `IsActive` / `UnsubscribedAt` |
| `DELETE /api/admin/alerts/subscribers/{id}` | Hard delete (POPIA erasure / right-to-be-forgotten) |
| `GET /api/admin/alerts/summary` | Counts: total, active, clicked, CTR, by-interest (optional; or fold into dashboard) |
| `GET /api/admin/alerts/subscribers/export` | CSV export (optional, phase 2) |

**Channel URL is managed through the existing settings controller** — no new
endpoint: `GET/PUT /api/admin/settings/communication` already handles the
`communication` group (`SettingsController.cs`). Admin edits `whatsapp_channel_url`
there.

---

## 4. Website UI placement and fields

**New page** `HypeGridWebsite/src/pages/Alerts.jsx` (route `/alerts`), styled like
`Contact.jsx`. Entry points: nav link, a Home hero CTA, and a banner on the deals
area.

**Fields** (one short form):

- Full name (optional)
- Email (optional) and/or WhatsApp number (optional) — client validates **at least
  one**
- City / Province (optional dropdowns)
- Interests (multi-select chips = deal categories)
- **Consent checkbox — unchecked by default, required** ([§6](#6-consent-text-recommendation))
- Submit button (the **active submit** — nothing is stored until they click it)

**Flow:**

1. User completes the form, ticks consent, clicks **Submit** →
   `POST /api/public/alerts/subscribe`.
2. On success, swap the form for a success panel: *"You're on the list."* plus the
   **CTA button "Join the HypeGrid WhatsApp Channel"**.
3. Clicking the CTA → `POST /api/public/alerts/{id}/channel-click`
   (fire-and-forget) **then** opens `whatsapp_channel_url` in a new tab.
4. Copy must **not** claim they've joined — only that they're subscribed to alerts
   and can tap to join. The channel link is the only join path; we record the
   *click*, not a *join*.

The CTA URL comes from the subscribe response (or `site-settings`); never hardcode
it in the frontend.

---

## 5. Admin page design

**New page** `HypeGridAdmin/src/pages/AlertSubscribers.jsx` (nav: "Alerts"), styled
like `Requests.jsx` / the newsletter list, reusing existing table / StatCard
components.

- **Stat cards:** Total subscribers · Active · Channel-clicks · Click-through rate.
- **Table columns:** Name · Email · Phone · Interests · City · Consent date ·
  Channel clicked (yes + date) · Status · Created.
- **Filters:** status (Active/OptedOut), clicked (yes/no), interest.
- **Row actions:** set status (Active/OptedOut), view detail (shows consent
  snapshot / version / timestamp for audit), delete (POPIA erasure with confirm).
- **Export CSV** button (phase 2).
- Wire into admin routing + sidebar nav alongside the existing marketing pages.

---

## 6. Consent text recommendation

A checkbox, **unticked by default**, required to submit. Store the shown text + a
version tag on the record.

**Primary consent (required):**

> ☐ I agree that HypeGrid may store the details I've provided and send me relevant
> deals, specials, and campaign alerts. I understand I can opt out at any time. See
> HypeGrid's Privacy Policy.

**Microcopy under the WhatsApp CTA (informational, not a second consent):**

> Tapping "Join" opens HypeGrid's WhatsApp Channel in WhatsApp. Joining a channel
> is managed by WhatsApp/Meta under their terms — HypeGrid only records that you
> tapped the button, not your WhatsApp identity.

**Future direct-messaging consent (only when the Meta API ships — separate,
unticked):**

> ☐ I agree to receive HypeGrid alerts as direct WhatsApp messages to the number
> above. Reply STOP at any time to opt out.

Notes: HypeGrid operates in South Africa → align with **POPIA** (lawful, specific,
informed, voluntary consent; purpose limitation; right to withdraw / erasure).
Versioning the consent copy (`ConsentTextVersion`) lets you prove *which* wording
each person accepted, and `ConsentSnapshot` stores the literal text.

---

## 7. Environment / config for the WhatsApp Channel URL

The channel URL is a **public, non-secret value** — do **not** put it in env vars /
secrets. Two layers, recommend both:

1. **Primary (admin-editable, no redeploy):** a `SiteSetting` row —
   `Key="whatsapp_channel_url"`, `Group="communication"`. It is then automatically
   returned by the existing public `GET /api/public/site-settings` (which already
   exposes the `communication` group) and editable via admin Settings. Seed a
   default in `HypeGridSeeder`.
2. **Optional fallback default:** an `appsettings.json` key, e.g.
   `HypeGridAlerts:WhatsAppChannelUrl`, used only if the SiteSetting is missing.

No new application-pool / environment variable is required for phase 1. (Meta API
secrets come later — [§9](#9-future-meta-whatsapp-business-api-upgrade-path).)

---

## 8. Analytics events to track

Reuse the existing anonymous pipeline — `POST /api/public/analytics/events` →
`AnalyticsEvent` (`PublicAnalyticsController.cs`). No personal data; IP hashed
server-side.

| Event | event_type | entity_type | When |
|---|---|---|---|
| Alerts form viewed | `impression` | `Other` (or new `AlertSubscription`) | Alerts page/section renders |
| Subscribed | `alert_subscribe` *(new)* or `cta_click` | `Other` / `AlertSubscription` | subscribe succeeds |
| Channel CTA clicked | **`whatsapp_click`** (already exists) | `Other` / `AlertSubscription` | user taps "Join" |

**Coordination with the parallel analytics session:** `whatsapp_click` already
exists in `AnalyticsEventTypes`. To track subscribe + a dedicated entity, add
`"alert_subscribe"` to `AnalyticsEventTypes` and `"AlertSubscription"` to
`AnalyticsEntityTypes` in `HypeGridValues.cs` — **but that file is owned by their
branch**, so make these additions *after* their merge (or hand them the two
literals). Until then, reuse `whatsapp_click` + `"Other"`, which need no changes.
Per-subscriber click truth lives on `AlertSubscriber.ChannelJoinClicked`; analytics
is the aggregate view.

---

## 9. Future Meta WhatsApp Business API upgrade path

The phase-1 schema is intentionally forward-compatible — the upgrade is additive,
with no rework.

1. **Provisioning:** WhatsApp Business Account + sender number via Meta Cloud API or
   a BSP (360dialog / Twilio). Store credentials (access token, phone-number-id) as
   **secrets via env vars / app-pool**, exactly like the SMTP `__Password` pattern —
   never in `appsettings.json`.
2. **Provider abstraction mirroring email:** add `IWhatsAppProvider` (Application) +
   `MetaWhatsAppProvider` (Infrastructure), config-driven, returns `Result`, never
   throws — directly parallel to `IEmailProvider` / `SmtpEmailProvider`. A boot-time
   readiness reporter can mirror `EmailSenderConfigReporter`.
3. **Verified, separate consent:** flip `DirectMessageOptIn` only via an explicit
   second opt-in; verify number ownership (OTP) before first send. Channel
   membership never implies messaging consent.
4. **Compliance plumbing:** approved message templates, the 24-hour session window,
   STOP-keyword handling (→ `IsActive=false`, `Status=OptedOut`), and delivery /
   opt-out webhooks.
5. **Already captured now that makes this easy:** E.164 phone, consent timestamp /
   version / text snapshot, source page, IP-hash audit. You will have a clean,
   provable opt-in audience the day the API goes live.

Public WhatsApp **Channel** (broadcast) and **1:1 messaging** stay as separate
products with separate consents throughout.

---

## 10. Implementation order (after the hero/deals/analytics branch merges)

0. **Wait for the parallel Marketing/Analytics branch to merge.** Rebase onto it
   before starting — avoids the conflicts listed in
   [merge-conflict hotspots](#merge-conflict-hotspots).
1. **Domain + DB:** `AlertSubscriber` entity → `AlertsConfigurations.cs` → `DbSet`
   on `AppDbContext` → **one new migration** (generate *after* the merge so the
   model snapshot is clean).
2. **Application:** `AlertSubscribeDto` + `IAlertSubscriptionService` / impl
   (persist-first, best-effort, de-dupe); register in DI.
3. **Public API:** add the three `alerts/*` endpoints to `PublicController` (or a
   small dedicated `AlertsController`).
4. **Config / seed:** seed `whatsapp_channel_url` SiteSetting (communication group);
   optional appsettings fallback.
5. **Admin API:** `AlertsAdminController` (+ optional summary).
6. **Admin UI:** `AlertSubscribers.jsx` + nav / route.
7. **Website UI:** `Alerts.jsx` page + Home/deals CTA + consent + analytics wiring;
   CTA reads the URL from the API.
8. **Analytics constants:** add `alert_subscribe` / `AlertSubscription` to
   `HypeGridValues` (coordinate with the analytics session); add an optional
   dashboard widget.
9. **QA + compliance:** consent-required enforcement, opt-out path, POPIA review of
   copy + erasure (DELETE), and verify we only ever store `ChannelJoinClicked` and
   never claim a join.
