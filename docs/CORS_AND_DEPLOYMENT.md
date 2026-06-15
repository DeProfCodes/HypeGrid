# CORS & Deployment (backend)

When the website and admin portal are deployed to Vercel, the backend API must
allow their browser origins in CORS, or the frontend's requests (content GETs,
form POSTs, and admin auth) will be blocked.

## How CORS is configured

CORS lives in `HypeGrid.API/Extensions/ServiceExtensions.cs` → `AddCustomCors`,
policy name `FrontendCors`.

- **Development** (`ASPNETCORE_ENVIRONMENT=Development`): any origin is allowed,
  so local Vite dev servers work with no setup.
- **Production**: only an explicit allow-list of origins is permitted, with
  credentials enabled (needed for the admin's auth flow).

The production allow-list is the built-in defaults **plus** any origins supplied
via configuration/environment, so new domains and Vercel preview URLs can be
added **without recompiling**.

## Configuring allowed origins (env / config)

Provide a comma- or semicolon-separated list of additional origins via either:

- Environment variable: `HYPEGRID_CORS_ORIGINS`
- Or `appsettings` key: `Cors:AllowedOrigins`

Example (environment):

```
HYPEGRID_CORS_ORIGINS=https://hypegrid.co.za,https://www.hypegrid.co.za,https://portal.hypegrid.co.za,https://admin.hypegrid.co.za
```

These are merged with the built-in defaults (deduplicated). Origins must include
the scheme and no trailing slash (e.g. `https://hypegrid.co.za`, not
`https://hypegrid.co.za/`).

## Expected production origins

| App     | Origin(s)                                                  |
| ------- | ---------------------------------------------------------- |
| Website | `https://hypegrid.co.za`, `https://www.hypegrid.co.za`     |
| Admin   | `https://portal.hypegrid.co.za`, `https://admin.hypegrid.co.za` |

## Vercel preview deployments

Each Vercel preview build gets a generated `*.vercel.app` origin. If forms or
admin login must work from previews, add those specific preview URLs to
`HYPEGRID_CORS_ORIGINS`. (CORS does not support wildcard subdomains together with
`AllowCredentials`, so list the exact preview origins you need.)

## Checklist before going live

1. Deploy the backend with `ASPNETCORE_ENVIRONMENT=Production`.
2. Set `HYPEGRID_CORS_ORIGINS` to the real website + portal origins.
3. Point each frontend's `VITE_HYPEGRID_API_BASE_URL` at the deployed API.
4. Verify a website form submit and an admin login from the deployed domains
   (check the browser console for CORS errors).
