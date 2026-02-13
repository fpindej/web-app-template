import { createApiClient, getErrorMessage } from '$lib/api';
import { error, redirect } from '@sveltejs/kit';
import { hasPermission, Permissions } from '$lib/utils';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, url, params, parent }) => {
	const { user } = await parent();
	if (!hasPermission(user, Permissions.Roles.View)) {
		throw redirect(303, '/');
	}

	const client = createApiClient(fetch, url.origin);

	const [roleResult, permissionsResult] = await Promise.all([
		client.GET('/api/v1/admin/roles/{id}', { params: { path: { id: params.id } } }),
		client.GET('/api/v1/admin/permissions')
	]);

	if (!roleResult.response.ok) {
		throw error(
			roleResult.response.status,
			getErrorMessage(roleResult.error, 'Failed to load role')
		);
	}

	return {
		role: roleResult.data,
		permissionGroups: permissionsResult.data ?? []
	};
};
