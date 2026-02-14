/**
 * Client-side permission utilities.
 * Mirrors backend AppPermissions constants.
 */

import type { User } from '$lib/types';

export const Permissions = {
	Users: {
		View: 'users.view',
		Manage: 'users.manage',
		AssignRoles: 'users.assign_roles'
	},
	Roles: {
		View: 'roles.view',
		Manage: 'roles.manage'
	},
	Jobs: {
		View: 'jobs.view',
		Manage: 'jobs.manage'
	}
} as const;

/** Returns true if the user has a specific permission. */
export function hasPermission(user: User | null | undefined, permission: string): boolean {
	return user?.permissions?.includes(permission) ?? false;
}

/** Returns true if the user has at least one of the given permissions. */
export function hasAnyPermission(user: User | null | undefined, permissions: string[]): boolean {
	return permissions.some((p) => hasPermission(user, p));
}
