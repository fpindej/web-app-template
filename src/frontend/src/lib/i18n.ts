import { register, init, getLocaleFromNavigator, locale, t as originalT } from 'svelte-i18n';
import { browser } from '$app/environment';
import type { TranslationKey } from './types/i18n';
import type { Readable } from 'svelte/store';

register('en', () => import('./locales/en.json'));
register('cs', () => import('./locales/cs.json'));

const defaultLocale = 'en';

export function initI18n(serverLocale?: string) {
	if (!browser) {
		init({ fallbackLocale: defaultLocale, initialLocale: serverLocale || defaultLocale });
		return;
	}

	const savedLocale = localStorage.getItem('locale');
	const browserLocale = getLocaleFromNavigator();

	// Simple logic: if it starts with 'cs', use 'cs', otherwise default to 'en'
	// You can expand this logic if you add more languages
	let initialLocale = savedLocale || serverLocale;

	if (!initialLocale) {
		if (browserLocale?.startsWith('cs')) {
			initialLocale = 'cs';
		} else {
			initialLocale = 'en';
		}
	}

	init({
		fallbackLocale: defaultLocale,
		initialLocale: initialLocale
	});
}

export function setLanguage(newLocale: string) {
	locale.set(newLocale);
	if (browser) {
		localStorage.setItem('locale', newLocale);
		document.cookie = `locale=${newLocale}; path=/; max-age=31536000; SameSite=Lax`;
		document.documentElement.setAttribute('lang', newLocale);
	}
}

export const t = originalT as unknown as Readable<
	(key: TranslationKey, vars?: Record<string, unknown>) => string
>;
