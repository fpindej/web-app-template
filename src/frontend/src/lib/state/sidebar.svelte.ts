/**
 * Reactive sidebar state for collapse/expand functionality.
 * Persists preference to localStorage.
 */

const STORAGE_KEY = 'sidebar-collapsed';

function getInitialState(): boolean {
	if (typeof window === 'undefined') return false;
	const stored = localStorage.getItem(STORAGE_KEY);
	return stored === 'true';
}

let collapsed = $state(false);

/**
 * Initialize sidebar state from localStorage.
 * Call this in onMount to avoid SSR hydration mismatch.
 */
export function initSidebar(): void {
	collapsed = getInitialState();
}

/**
 * Check if the sidebar is currently collapsed.
 */
export function isCollapsed(): boolean {
	return collapsed;
}

/**
 * Toggle the sidebar collapsed state.
 */
export function toggleSidebar(): void {
	collapsed = !collapsed;
	if (typeof window !== 'undefined') {
		localStorage.setItem(STORAGE_KEY, String(collapsed));
	}
}

/**
 * Set the sidebar collapsed state explicitly.
 */
export function setSidebarCollapsed(value: boolean): void {
	collapsed = value;
	if (typeof window !== 'undefined') {
		localStorage.setItem(STORAGE_KEY, String(collapsed));
	}
}
