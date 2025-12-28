import createClient from 'openapi-fetch';
import type { paths } from './v1';

export const createApiClient = (customFetch?: typeof fetch, baseUrl: string = '') => {
	let refreshPromise: Promise<Response> | null = null;

	const fetchWithAuth: typeof fetch = async (input, init) => {
		const f = customFetch || fetch;
		const response = await f(input, init);

		if (response.status === 401) {
			const url =
				typeof input === 'string' ? input : input instanceof URL ? input.toString() : input.url;

			if (url.includes('/api/auth/refresh')) {
				return response;
			}

			if (!refreshPromise) {
				const refreshUrl = baseUrl ? `${baseUrl}/api/auth/refresh` : '/api/auth/refresh';
				refreshPromise = f(refreshUrl, {
					method: 'POST'
				}).then((res) => {
					refreshPromise = null;
					return res;
				});
			}

			const refreshResponse = await refreshPromise;

			if (refreshResponse && refreshResponse.ok) {
				// Retry the original request
				return f(input, init);
			}
		}

		return response;
	};

	return createClient<paths>({
		baseUrl,
		fetch: fetchWithAuth
	});
};

/**
 * Client for browser-side usage only.
 * For server-side (load functions), use createApiClient(fetch).
 */
export const browserClient = createApiClient();
