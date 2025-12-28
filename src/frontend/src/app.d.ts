import type { components } from '$lib/api/v1';

declare global {
	namespace App {
		interface Locals {
			user: components['schemas']['MeResponse'] | null;
			locale: string;
		}
	}
}

export {};
