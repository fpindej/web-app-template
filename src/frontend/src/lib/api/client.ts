import createClient from 'openapi-fetch';
import type { paths } from './v1';

export const createApiClient = (customFetch?: typeof fetch, baseUrl: string = '') =>
	createClient<paths>({
		baseUrl,
		fetch: customFetch
	});

/**
 * Client for browser-side usage only.
 * For server-side (load functions), use createApiClient(fetch).
 */
export const browserClient = createApiClient();
