# HypeGrid — Frontend Integration Notes

How to move `HypeGridWebsite` and `HypeGridAdmin` off the Base44 SDK and onto
this API. **No redesign** — only the data layer changes. Field names already
match (the API serializes snake_case: `brand_name`, `created_date`, `id`, …),
so component code that reads entity fields keeps working.

## 1. Replace the Base44 client with an HTTP client

Both apps load `src/api/base44Client.js` and call `base44.entities.X.list()`,
`base44.auth.*`, etc. Swap that module for a thin `fetch`/`axios` client:

```js
// src/api/client.js
const BASE = import.meta.env.VITE_API_BASE_URL; // e.g. http://localhost:5249
function authHeaders() {
  const t = localStorage.getItem("hg_access_token");
  return t ? { Authorization: `Bearer ${t}` } : {};
}
export async function api(path, { method = "GET", body } = {}) {
  const res = await fetch(`${BASE}${path}`, {
    method,
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: body ? JSON.stringify(body) : undefined,
  });
  const json = await res.json().catch(() => ({}));
  if (!res.ok || json.success === false) throw new Error(json.message || res.statusText);
  return json.data; // unwrap the { success, message, data } envelope
}
```

Add `VITE_API_BASE_URL` to each app's `.env`. Then provide a tiny entity shim so
existing call sites barely change:

```js
// base44 -> api shim
const entity = (base) => ({
  list: (sort = "-created_date", limit = 100) => api(`${base}?sort=${sort}&limit=${limit}`),
  filter: (q) => api(`${base}?${new URLSearchParams(q)}`),
  get: (id) => api(`${base}/${id}`),
  create: (data) => api(base, { method: "POST", body: data }),
  update: (id, data) => api(`${base}/${id}`, { method: "PUT", body: data }),
  delete: (id) => api(`${base}/${id}`, { method: "DELETE" }),
});
export const entities = {
  Campaign: entity("/api/admin/campaigns"),
  CampaignRequest: entity("/api/admin/campaign-requests"),
  Client: entity("/api/admin/clients"),
  Creator: entity("/api/admin/creators"),
  Deliverable: entity("/api/admin/deliverables"),
  Task: entity("/api/admin/tasks"),
  Note: entity("/api/admin/notes"),
  Quote: entity("/api/admin/quotes"),
  Invoice: entity("/api/admin/invoices"),
  Payout: entity("/api/admin/payouts"),
  User: { list: () => api("/api/admin/users") },
};
```

---

## 2. WEBSITE (`HypeGridWebsite`) — files to update

| File | Today | Change |
|---|---|---|
| `src/pages/Contact.jsx` | Form only sets `submitted=true`, no backend | On submit `POST /api/public/contact` with `{ full_name, email, phone, subject, interest, message, source_page:"contact" }`; keep the existing success screen |
| `src/pages/Campaigns.jsx` | Form only sets local state | `POST /api/public/campaign-requests` with `{ full_name, brand_name, email, phone, promote_what, campaign_type, target_audience, platforms[], budget, goal, message }` |
| `src/pages/Creators.jsx` | Form only sets local state | `POST /api/public/creator-applications` with `{ full_name, email, phone, city, platform, handle, followers, niche, why }` |
| `src/pages/Services.jsx`, `components/home/ServicesPreview.jsx` | Hardcoded 8-service array | Optionally `GET /api/public/services` (already seeded with the same 8). Safe to leave static if you prefer — content is identical |
| `src/pages/Packages.jsx`, `components/home/PackagesPreview.jsx` | Hardcoded 5 packages | Optionally `GET /api/public/packages` (seeded, `is_featured` set on Growth Hype) |
| `src/lib/AuthContext.jsx`, `pages/Login/Register/Forgot/Reset` | `base44.auth.*` | Point at `/api/auth/*` (login/register/refresh/me/forgot-password/reset-password). OTP-based register is not in phase 1 — use email/password or hide register on the public site |
| `components/home/PortfolioPreview.jsx` | "showcase coming soon" placeholders | `GET /api/public/case-studies` when you start adding them via admin |

Static homepage copy (hero stats, "what we promote", pillars, HypeGrid effect)
has **no API yet** — it can stay hardcoded or be promoted to `SiteSetting`/new
content entities later (see README open item #3).

---

## 3. ADMIN (`HypeGridAdmin`) — files to update

All admin pages use `base44.entities.*`. With the shim above, the swaps are mechanical:

| Page | Base44 calls | New endpoints |
|---|---|---|
| `pages/Dashboard.jsx` + `components/dashboard/DashboardCharts.jsx` | Lists 7 entities and computes stats client-side | Replace with `GET /api/admin/dashboard/summary`, `/recent-campaign-requests`, `/active-campaigns`, `/pending-deliverables`, `/campaigns-by-status`, `/campaigns-by-type`, `/leads-by-type`, `/monthly-leads` (all server-computed) |
| `pages/Campaigns.jsx` | `Campaign.list/create/update` | `/api/admin/campaigns` CRUD; `PATCH …/{id}/status`, `…/{id}/progress` |
| `pages/CampaignDetail.jsx` | Lists Deliverable/Task/Invoice/Note filtered by name | `GET /api/admin/campaigns/{id}/tasks|deliverables|notes|invoices` |
| `pages/Clients.jsx`, `ClientDetail.jsx` | `Client.*`, related lists | `/api/admin/clients` CRUD; `GET …/{id}/campaigns|quotes|invoices` |
| `pages/Creators.jsx`, `CreatorDetail.jsx` | `Creator.*`, deliverables, payouts | `/api/admin/creators` CRUD; `PATCH …/{id}/status`; `GET …/{id}/deliverables|payouts` |
| `pages/Deliverables.jsx` | `Deliverable.list/update` (approve / changes) | `/api/admin/deliverables`; `POST …/{id}/approve`, `…/request-changes`, `…/mark-posted` |
| `pages/Requests.jsx` | `CampaignRequest.list/update` | `/api/admin/campaign-requests`; `PATCH …/{id}/status|assign`; `POST …/{id}/convert-to-client|convert-to-campaign` |
| `pages/Quotes.jsx` | `Quote.list/create` | `/api/admin/quotes`; `POST …/{id}/convert-to-invoice` |
| `pages/Payments.jsx` | `Invoice.*` (mark paid) | `/api/admin/invoices`; `POST …/{id}/record-payment` |
| `pages/Payouts.jsx` | `Payout.*` (approve / mark paid) | `/api/admin/payouts`; `POST …/{id}/mark-paid`; `PATCH …/{id}/status` |
| `pages/Messages.jsx` | `Note.list/create` | `/api/admin/notes` |
| `pages/Team.jsx` | `User.list` | `GET /api/admin/users` (+ create / role / status for SuperAdmin) |
| `pages/Settings.jsx` | Hardcoded values | `GET/PUT /api/admin/settings/{company|communication|finance}` |
| `pages/Reports.jsx` | Lists + client-side aggregation | Reuse dashboard widgets, or extend with `/api/admin/dashboard/*` |
| `pages/CalendarPage.jsx` | Campaign + Deliverable lists | `GET /api/admin/campaigns` + `/api/admin/deliverables` (filter client-side by date as today) |
| `lib/AuthContext.jsx`, auth pages | `base44.auth.*` | `/api/auth/*`; store `access_token`/`refresh_token`; send `Authorization: Bearer` |

### Notes
- The admin Creators page "Payouts Due" card is hardcoded `0` today; the
  `/api/admin/dashboard/summary` `payouts_due` field gives the real number.
- Conversions (`request → client/campaign`, `application → creator`) are new
  server actions — wire them to the existing "Convert" buttons.
- `Task` entity is exposed at `/api/admin/tasks` (DB table `Tasks`); the JSON
  shape is unchanged from Base44.
