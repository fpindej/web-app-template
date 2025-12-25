import type { LayoutServerLoad } from './$types';
import { SERVER_CONFIG } from '$lib/server/config';

export const load: LayoutServerLoad = async ({ locals }) => {
	return { user: locals.user, apiUrl: SERVER_CONFIG.API_URL, locale: locals.locale };
};
