# Security

Security is the highest priority in every development decision. When faced with a trade-off between convenience and security, always choose security. When unsure whether something is safe, assume it isn't and investigate.

## Guiding Principles

| Principle | What It Means in Practice |
|---|---|
| **Restrictive by default** | Deny access, disable features, block origins, strip headers — then selectively open what's needed. Never start permissive and try to lock down later. |
| **Defense in depth** | Don't rely on a single layer. Validate on both frontend and backend. Check auth in middleware *and* controllers. Use security headers *and* CSP. |
| **Least privilege** | Services, tokens, cookies, and API responses should expose the minimum data and permissions required. |
| **Fail closed** | If validation fails, token parsing fails, or an origin check fails — reject the request. Never fall through to a permissive default. |
| **Secrets never in code** | Connection strings, API keys, JWT secrets — always in `.env` or environment variables, never in source. Rotate compromised secrets immediately. |
| **Audit new dependencies** | Before adding a NuGet package or npm module, consider its attack surface. Prefer well-maintained, minimal-dependency libraries. |

## When Building Features

1. **Think about abuse first.** Before implementing, ask: how could this be exploited? What happens if the input is malicious? What if the user is unauthenticated?
2. **Validate all input.** Never trust client data — validate on the backend even if the frontend already validates. Use FluentValidation for complex rules, Data Annotations for simple constraints.
3. **Sanitize all output.** Prevent XSS by escaping user-generated content. Never render raw HTML from user input. Validate URLs to block `javascript:` schemes.
4. **Protect state-changing operations.** All mutations (POST/PUT/DELETE) must verify authentication, authorization, and CSRF protection.
5. **Log security events.** Failed login attempts, token refresh failures, authorization denials — log at Warning/Error level for monitoring.

---

## Authentication Architecture

### Cookie-Based JWT

The backend issues JWT access tokens and refresh tokens, stored in **HttpOnly cookies**. The browser never sees or handles JWT tokens directly — they're set via `Set-Cookie` headers and forwarded automatically on every request.

**Why HttpOnly cookies over Authorization headers:**

- **XSS protection** — JavaScript cannot read HttpOnly cookies. If an attacker achieves XSS, they can't steal the token.
- **Automatic forwarding** — no frontend code needed to attach tokens to requests. Cookies are sent automatically.
- **Refresh transparency** — the refresh token is also in a cookie, so token rotation happens transparently through the API proxy.

**Cookie attributes:**

| Attribute | Value | Purpose |
|---|---|---|
| `HttpOnly` | `true` | Prevents JavaScript access |
| `Secure` | `true` | Only sent over HTTPS |
| `SameSite` | `None` | Required for cross-site cookie forwarding through the proxy |
| `Path` | `/` (access) or `/api/auth/refresh` (refresh) | Limits cookie scope |

**Why `SameSite=None`:** The SvelteKit server and .NET API are different origins (different ports). The browser needs to send cookies cross-origin for the proxy to work. `SameSite=None` requires `Secure=true`, which means HTTPS is mandatory (except localhost, where browsers make an exception).

### Token Refresh

Refresh tokens are:
- **SHA-256 hashed** before database storage (PR #27) — a database breach doesn't expose valid refresh tokens
- **Rotated on every refresh** — the old token is invalidated, a new one is issued
- **Scoped** — the refresh token cookie is only sent on `/api/auth/refresh` requests, not on every API call

### Account Lockout

ASP.NET Identity account lockout is enabled (PR #31). After a configurable number of failed login attempts, the account is temporarily locked. This mitigates brute-force attacks.

---

## Security Response Headers

Both the backend and frontend apply security headers. The frontend applies them to page responses via `hooks.server.ts`; the backend applies them to API responses. API proxy routes are skipped on the frontend — they receive headers from the backend directly.

| Header | Value | Purpose |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | Prevents MIME-type sniffing — stops browsers from interpreting a response as a different content type than declared. Mitigates XSS through content type confusion. |
| `X-Frame-Options` | `DENY` | Prevents embedding in iframes. Mitigates clickjacking attacks where an attacker overlays a transparent iframe to capture clicks. |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Prevents leaking URL paths to third-party sites. Same-origin requests get the full URL; cross-origin requests only get the origin. |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` | Disables browser APIs the app doesn't use. `()` means "deny to all origins." If a feature needs a browser API (e.g., webcam for avatar capture), change the specific directive to `(self)` — never remove the header or use `*`. |
| `Strict-Transport-Security` | `max-age=63072000; includeSubDomains` | HSTS — tells browsers to always use HTTPS. **Production only** — enabling in development breaks `localhost` HTTP. |

---

## Content Security Policy (CSP)

CSP is configured via nonce mode in `svelte.config.js` using SvelteKit's built-in `kit.csp`.

### Directive Decisions

| Directive | Value | Rationale |
|---|---|---|
| `script-src` | `'self' 'nonce-...'` | Nonce-based. Only scripts with a per-request nonce can execute. The FOUC prevention script in `app.html` uses `%sveltekit.nonce%`. |
| `style-src` | `'self' 'unsafe-inline'` | Required because Svelte transitions (`fly`, `scale`) inject inline `<style>` elements at runtime. This is a [documented SvelteKit limitation](https://svelte.dev/docs/kit/configuration#csp). |
| `img-src` | `'self' https: data:` | `https:` allows external avatar URLs. `data:` is required because Vite inlines files under 4KB as data URIs at build time — this affects the favicon SVG and flag-icons CSS sprites. |
| `frame-ancestors` | `'none'` | Defense-in-depth alongside `X-Frame-Options: DENY`. |

### Why Nonce Over Hash

- **Nonce** — per-request random value, works with dynamic content, SvelteKit generates it automatically.
- **Hash** — requires knowing script content at build time, breaks with any dynamic content.
- **`unsafe-inline`** — defeats the purpose of CSP entirely for scripts.

We chose nonce because SvelteKit has first-class nonce support via `kit.csp`, and it handles the complexity of injecting nonces into generated script tags automatically.

### Why `unsafe-inline` for Styles

Svelte's transition system (`transition:fly`, `transition:scale`, etc.) dynamically creates `<style>` elements at runtime. These inline styles have no nonce and no predictable hash. SvelteKit's documentation explicitly states that `style-src` requires `unsafe-inline` when using transitions.

This is an accepted tradeoff — CSS injection is significantly less dangerous than script injection. The attack surface for style-based exfiltration (CSS injection) exists but is narrow and requires specific conditions.

---

## CSRF Protection

### SvelteKit API Proxy

The API proxy at `routes/api/[...path]/+server.ts` validates the `Origin` header on state-changing requests (POST/PUT/PATCH/DELETE). This is necessary because SvelteKit's built-in CSRF protection only covers form actions, not `+server.ts` routes.

The proxy allows:
1. **Same-origin requests** — `Origin` matches `url.origin`
2. **Configured origins** — `Origin` matches an entry in `ALLOWED_ORIGINS` env var
3. **Missing `Origin` header** — safe to allow (same-origin older browsers or non-browser clients)

Requests with a mismatched `Origin` are rejected with 403.

### Backend

The backend validates JWT tokens on every request. Since JWTs are in HttpOnly cookies (not Authorization headers), CSRF is a concern. The defense layers are:
1. **`SameSite=None` + `Secure`** on cookies — requires HTTPS
2. **Origin validation** at the proxy level
3. **JWT validation** at the backend level — even if an attacker triggers a cross-origin request, the JWT must be valid

---

## Header Filtering

The API proxy allowlists which request and response headers pass through. This prevents:

- **Request header injection** — malicious frontend code can't inject arbitrary headers to the backend
- **Response header leakage** — internal backend headers (server version, debug info) aren't exposed to the browser

---

## Secret Management

All secrets are in `.env` files (git-ignored). The `.env.example` file contains working defaults with a static dev JWT key. The init script generates a random JWT secret for local development.

**Never commit:**
- `.env` files
- Connection strings with real credentials
- JWT signing keys
- API keys or tokens

If a secret is accidentally committed, rotate it immediately — removing it from git history is not sufficient (the secret is in the reflog and any forks).
