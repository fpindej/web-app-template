import type { components } from '$lib/api/v1';

/**
 * Shared type aliases for commonly used API types.
 * Centralizes type definitions to avoid repetition across components.
 */
export type User = components['schemas']['UserResponse'];
