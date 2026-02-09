import type { LayoutServerLoad } from './$types';
import { dev } from '$app/environment';
import { SERVER_CONFIG } from '$lib/config/server';
import { getUser } from '$lib/auth';

export const load: LayoutServerLoad = async ({ locals, fetch, url }) => {
	const user = await getUser(fetch, url.origin);
	return { user, locale: locals.locale, apiUrl: dev ? SERVER_CONFIG.API_URL : undefined };
};
