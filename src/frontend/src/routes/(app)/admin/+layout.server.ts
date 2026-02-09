import { redirect } from '@sveltejs/kit';
import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = async ({ parent }) => {
	const { user } = await parent();

	if (!user) {
		throw redirect(303, '/login');
	}

	const isAdmin = user.roles?.some((r) => r === 'Admin' || r === 'SuperAdmin') ?? false;
	if (!isAdmin) {
		throw redirect(303, '/');
	}

	return { user };
};
