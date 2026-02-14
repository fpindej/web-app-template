import { createApiClient, getErrorMessage } from '$lib/api';
import { error, redirect } from '@sveltejs/kit';
import { hasPermission, Permissions } from '$lib/utils';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, url, params, parent }) => {
	const { user } = await parent();
	if (!hasPermission(user, Permissions.Jobs.View)) {
		throw redirect(303, '/');
	}

	const client = createApiClient(fetch, url.origin);

	const {
		data,
		response,
		error: apiError
	} = await client.GET('/api/v1/admin/jobs/{jobId}', {
		params: { path: { jobId: params.jobId } }
	});

	if (!response.ok) {
		throw error(response.status, getErrorMessage(apiError, 'Failed to load job details'));
	}

	return {
		job: data
	};
};
