import { browser } from '$app/environment';

/**
 * Detect if the user is on a Mac/iOS device.
 * Uses navigator.userAgentData.platform when available (modern browsers),
 * with navigator.platform fallback (deprecated but more widely supported).
 */
function getPlatform(): string {
	if (!browser) return '';
	// Modern Chromium browsers expose userAgentData
	if ('userAgentData' in navigator && navigator.userAgentData) {
		return (navigator.userAgentData as { platform?: string }).platform ?? '';
	}
	return navigator.platform ?? '';
}

const platform = getPlatform();

export const IS_MAC = browser
	? /mac/i.test(platform) ||
		/iPhone|iPad|iPod/.test(navigator.platform) ||
		// Fallback for iPad with desktop mode (reports MacIntel but has touch)
		(navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)
	: false;

export const IS_WINDOWS = browser ? /win/i.test(platform) : false;
