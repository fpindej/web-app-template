import { browser } from '$app/environment';

/**
 * Detect if the user is on a Mac/iOS device.
 * Uses navigator.platform (more reliable for keyboard detection) with userAgent fallback.
 */
export const IS_MAC = browser
	? /Mac|iPhone|iPad|iPod/.test(navigator.platform) ||
		// Fallback for iPad with desktop mode (reports MacIntel but has touch)
		(navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)
	: false;

export const IS_WINDOWS = browser ? /Win/.test(navigator.platform) : false;
