import type { Handle } from '@sveltejs/kit';
import { paraglideMiddleware } from '$lib/paraglide/server';
import { extractLocaleFromHeader, cookieName, baseLocale } from '$lib/paraglide/runtime';

/** Security headers applied to all page responses (not API proxy routes). */
const SECURITY_HEADERS: Record<string, string> = {
	'X-Content-Type-Options': 'nosniff',
	'X-Frame-Options': 'DENY',
	'Referrer-Policy': 'strict-origin-when-cross-origin',
	'Permissions-Policy': 'camera=(), microphone=(), geolocation=()'
};

export const handle: Handle = async ({ event, resolve }) => {
	// API proxy routes — backend sets its own security headers
	if (event.url.pathname.startsWith('/api')) {
		return resolve(event);
	}

	return paraglideMiddleware(event.request, async ({ locale }) => {
		const cookieLocale = event.cookies.get(cookieName);
		if (!cookieLocale && locale === baseLocale) {
			const headerLocale = extractLocaleFromHeader(event.request);
			if (headerLocale) {
				locale = headerLocale;
			}
		}

		event.locals.locale = locale;
		event.locals.user = null;

		const response = await resolve(event, {
			transformPageChunk: ({ html }) => html.replace('%lang%', locale)
		});

		for (const [header, value] of Object.entries(SECURITY_HEADERS)) {
			response.headers.set(header, value);
		}

		return response;
	});
};
