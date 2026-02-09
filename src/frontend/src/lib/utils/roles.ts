/**
 * Client-side role hierarchy utilities.
 * Mirrors the backend AppRoles.GetRoleRank() logic.
 */

const ROLE_RANKS: Record<string, number> = {
	SuperAdmin: 3,
	Admin: 2,
	User: 1
};

/** Returns the numeric rank for a role name. Unknown roles return 0. */
export function getRoleRank(role: string): number {
	return ROLE_RANKS[role] ?? 0;
}

/** Returns the highest rank from a list of role names. */
export function getHighestRank(roles: string[]): number {
	return Math.max(0, ...roles.map(getRoleRank));
}

/** Returns true if the caller's roles outrank the target's roles (strictly greater). */
export function canManageUser(callerRoles: string[], targetRoles: string[]): boolean {
	return getHighestRank(callerRoles) > getHighestRank(targetRoles);
}

/** Returns roles that are below the caller's highest rank (assignable by them). */
export function getAssignableRoles(callerRoles: string[], allRoleNames: string[]): string[] {
	const callerRank = getHighestRank(callerRoles);
	return allRoleNames.filter((role) => getRoleRank(role) < callerRank);
}
