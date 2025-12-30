import { goto, invalidateAll } from '$app/navigation';
import { resolve } from '$app/paths';
import { browserClient, createApiClient } from '$lib/api';
import type { User } from '$lib/types';

export async function getUser(
	fetch: typeof globalThis.fetch,
	origin: string
): Promise<User | null> {
	const client = createApiClient(fetch, origin);

	try {
		const { data: user, response } = await client.GET('/api/users/me');
		if (response.ok && user) {
			return user;
		}
	} catch (e) {
		console.error('Failed to fetch user:', e);
	}

	return null;
}

export async function logout() {
	await browserClient.POST('/api/auth/logout');
	await invalidateAll();
	await goto(resolve('/login'));
}
