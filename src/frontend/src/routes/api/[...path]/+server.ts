import type { RequestHandler } from './$types';
import { SERVER_CONFIG } from '$lib/config/server';
import { isFetchErrorWithCode } from '$lib/api';

/** Auth endpoints that need cookie-based auth for web clients */
const COOKIE_AUTH_ENDPOINTS = ['auth/login', 'auth/refresh'];

/** HTTP methods that can mutate state and are vulnerable to CSRF */
const UNSAFE_METHODS = ['POST', 'PUT', 'PATCH', 'DELETE'];

/**
 * Validates that the request Origin header matches the app's own origin.
 * This prevents cross-site request forgery for cookie-authenticated requests
 * proxied through SvelteKit (SameSite=None cookies are sent cross-origin).
 *
 * Only enforced for state-changing methods — GET/HEAD/OPTIONS pass through.
 * Does not affect mobile or API-key clients since they call the backend directly.
 */
function isOriginAllowed(request: Request, url: URL): boolean {
	if (!UNSAFE_METHODS.includes(request.method)) {
		return true;
	}

	const origin = request.headers.get('origin');

	// Browsers always send Origin on cross-origin requests and on same-origin
	// POST/PUT/PATCH/DELETE. A missing Origin on an unsafe method means either
	// a same-origin request from an older browser or a non-browser client —
	// both are safe to allow through.
	if (!origin) {
		return true;
	}

	return origin === url.origin;
}

export const fallback: RequestHandler = async ({ request, params, url, fetch }) => {
	if (!isOriginAllowed(request, url)) {
		return new Response(JSON.stringify({ message: 'Cross-origin requests are not allowed' }), {
			status: 403,
			headers: { 'Content-Type': 'application/json' }
		});
	}

	// Build target URL with query string
	const targetParams = new URLSearchParams(url.search);

	// Web clients need cookies for auth endpoints
	if (COOKIE_AUTH_ENDPOINTS.includes(params.path)) {
		targetParams.set('useCookies', 'true');
	}

	const queryString = targetParams.toString();
	const targetUrl = `${SERVER_CONFIG.API_URL}/api/${params.path}${queryString ? `?${queryString}` : ''}`;

	const newRequest = new Request(targetUrl, {
		method: request.method,
		headers: request.headers,
		body: request.body,
		// @ts-expect-error - duplex is needed for streaming bodies in some node versions/fetch implementations
		duplex: 'half'
	});

	newRequest.headers.delete('host');
	newRequest.headers.delete('connection');
	// Note: We are forwarding all other headers (including Cookie and Authorization)
	// This is necessary for auth to work, but be aware of the security implications
	// if the backend is not trusted or if this proxy is exposed to untrusted clients.

	try {
		const response = await fetch(newRequest);
		return response;
	} catch (err) {
		console.error('Proxy error:', err);

		if (isFetchErrorWithCode(err, 'ECONNREFUSED')) {
			return new Response(JSON.stringify({ message: 'Backend unavailable' }), {
				status: 503,
				headers: { 'Content-Type': 'application/json' }
			});
		}

		return new Response('Bad Gateway', { status: 502 });
	}
};
