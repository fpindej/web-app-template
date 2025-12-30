import { browser } from '$app/environment';
import { IS_MAC } from '$lib/utils/platform';
import * as m from '$lib/paraglide/messages';

// --- State ---

class ShortcutsState {
	isHelpOpen = $state(false);
}

export const shortcutsState = new ShortcutsState();

// --- Configuration ---

export const ShortcutAction = {
	Settings: 'settings',
	Logout: 'logout',
	Help: 'help',
	ToggleSidebar: 'toggleSidebar'
} as const;

export type ShortcutActionType = (typeof ShortcutAction)[keyof typeof ShortcutAction];

export interface ShortcutConfig {
	key: string;
	meta?: boolean; // Cmd on Mac, Ctrl on Windows/Linux
	shift?: boolean;
	alt?: boolean;
	ctrl?: boolean; // Explicit Ctrl
	action: ShortcutActionType;
	description: () => string;
}

const SHORTCUTS: ShortcutConfig[] = [
	{
		key: ',',
		meta: true,
		action: ShortcutAction.Settings,
		description: m.shortcuts_settings
	},
	{
		key: 'l',
		meta: true,
		shift: true,
		action: ShortcutAction.Logout,
		description: m.shortcuts_logout
	},
	{
		key: '[',
		meta: true,
		action: ShortcutAction.ToggleSidebar,
		description: m.shortcuts_toggleSidebar
	},
	{
		key: '?',
		shift: true,
		action: ShortcutAction.Help,
		description: m.shortcuts_help
	}
];

export function getAllShortcuts(): ShortcutConfig[] {
	return SHORTCUTS;
}

export function getShortcutSymbol(action: ShortcutActionType): string {
	if (!browser) return '';
	const config = SHORTCUTS.find((s) => s.action === action);
	if (!config) return '';

	const parts: string[] = [];

	if (config.meta) parts.push(IS_MAC ? '⌘' : 'Ctrl');
	if (config.ctrl) parts.push('Ctrl');
	if (config.alt) parts.push(IS_MAC ? '⌥' : 'Alt');
	if (config.shift) parts.push(IS_MAC ? '⇧' : 'Shift');

	const key = config.key.toUpperCase();
	parts.push(key);

	return parts.join(IS_MAC ? ' ' : '+');
}

// --- Action ---

// Map actions to handler functions directly
export type ShortcutHandlers = Partial<Record<ShortcutActionType, () => void>>;

export function globalShortcuts(node: Window, handlers: ShortcutHandlers = {}) {
	let currentHandlers = handlers;

	function handleKeydown(event: KeyboardEvent) {
		if (!browser) return;

		const target = event.target as HTMLElement;
		if (isInput(target)) return;

		for (const sc of SHORTCUTS) {
			// Map abstract modifiers to physical keys based on platform
			const pressedMeta = IS_MAC ? event.metaKey : event.ctrlKey;
			const pressedCtrl = IS_MAC ? event.ctrlKey : false; // Explicit Control (rare on Win if mapped to meta)

			if (!!sc.meta !== pressedMeta) continue;
			if (!!sc.ctrl !== pressedCtrl) continue;
			if (!!sc.shift !== event.shiftKey) continue;
			if (!!sc.alt !== event.altKey) continue;

			// Check key (case-insensitive to handle Shift+L vs l)
			if (event.key.toLowerCase() !== sc.key.toLowerCase()) continue;

			event.preventDefault();
			executeAction(sc.action, currentHandlers);
			return;
		}
	}

	window.addEventListener('keydown', handleKeydown);

	return {
		update(newHandlers: ShortcutHandlers) {
			currentHandlers = newHandlers;
		},
		destroy() {
			window.removeEventListener('keydown', handleKeydown);
		}
	};
}

function isInput(target: HTMLElement) {
	return (
		target.tagName === 'INPUT' ||
		target.tagName === 'TEXTAREA' ||
		target.tagName === 'SELECT' ||
		target.isContentEditable
	);
}

function executeAction(action: ShortcutActionType, handlers: ShortcutHandlers) {
	if (action === ShortcutAction.Help) {
		shortcutsState.isHelpOpen = !shortcutsState.isHelpOpen;
		return;
	}
	// Execute the handler if provided
	handlers[action]?.();
}
