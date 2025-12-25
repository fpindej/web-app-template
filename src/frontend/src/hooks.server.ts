import type { Handle } from '@sveltejs/kit';
import { createApiClient } from '$lib/api/client';
import { defaultLocale, supportedLocales } from '$lib/i18n';

export const handle: Handle = async ({ event, resolve }) => {
	// Skip auth check for API routes to avoid infinite loops
	if (event.url.pathname.startsWith('/api')) {
		return resolve(event);
	}

	const cookieLang = event.cookies.get('locale');
	const headerLang = event.request.headers.get('accept-language')?.split(',')[0];
	const lang = cookieLang || headerLang;

	const foundLocale = supportedLocales.find((l) => lang?.startsWith(l));
	const locale = foundLocale || defaultLocale;
	event.locals.locale = locale;

	const client = createApiClient(event.fetch, event.url.origin);

	try {
		const { data: user, response } = await client.GET('/api/auth/me');
		if (response.ok && user) {
			event.locals.user = user;
		} else {
			event.locals.user = null;
		}
	} catch (e) {
		console.error('Failed to fetch user:', e);
		event.locals.user = null;
	}

	return resolve(event, {
		transformPageChunk: ({ html }) => html.replace('%lang%', event.locals.locale)
	});
};
