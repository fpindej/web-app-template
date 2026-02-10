import { createApiClient, getErrorMessage } from '$lib/api';
import { error } from '@sveltejs/kit';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, url }) => {
	const client = createApiClient(fetch, url.origin);

	const { data, response, error: apiError } = await client.GET('/api/v1/admin/roles');

	if (!response.ok) {
		throw error(response.status, getErrorMessage(apiError, 'Failed to load roles'));
	}

	return {
		roles: data ?? []
	};
};
