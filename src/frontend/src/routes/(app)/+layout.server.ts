import { redirect } from '@sveltejs/kit';
import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = async ({ parent }) => {
	const { user } = await parent();

	if (!user) {
		throw redirect(303, '/login');
	}

	return {
		user
	};
};
