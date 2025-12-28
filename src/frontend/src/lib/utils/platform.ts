import { browser } from '$app/environment';

export const IS_MAC = browser ? /Mac|iPod|iPhone|iPad/.test(navigator.userAgent) : false;
export const IS_WINDOWS = browser ? /Win/.test(navigator.userAgent) : false;
