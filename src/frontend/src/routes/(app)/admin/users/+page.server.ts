import { createApiClient, getErrorMessage } from '$lib/api';
import { error } from '@sveltejs/kit';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, url }) => {
	const client = createApiClient(fetch, url.origin);

	const pageNumber = Number(url.searchParams.get('page') ?? '1');
	const pageSize = Number(url.searchParams.get('pageSize') ?? '10');
	const search = url.searchParams.get('search') ?? '';

	const {
		data,
		response,
		error: apiError
	} = await client.GET('/api/v1/admin/users', {
		params: {
			query: {
				pageNumber,
				pageSize,
				search: search || undefined
			}
		}
	});

	if (!response.ok) {
		throw error(response.status, getErrorMessage(apiError, 'Failed to load users'));
	}

	return {
		users: data,
		search
	};
};
