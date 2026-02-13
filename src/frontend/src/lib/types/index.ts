import type { components } from '$lib/api/v1';

/**
 * Shared type aliases for commonly used API types.
 * Centralizes type definitions to avoid repetition across components.
 */
export type User = components['schemas']['UserResponse'];
export type AdminUser = components['schemas']['AdminUserResponse'];
export type AdminRole = components['schemas']['AdminRoleResponse'];
export type ListUsersResponse = components['schemas']['ListUsersResponse'];
export type RoleDetail = components['schemas']['RoleDetailResponse'];
export type PermissionGroup = components['schemas']['PermissionGroupResponse'];
