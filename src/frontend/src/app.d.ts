import type { components } from '$lib/api/v1';

declare global {
	namespace App {
		interface Locals {
			user: components['schemas']['UserResponse'] | null;
			locale: string;
		}
	}
}

export {};
