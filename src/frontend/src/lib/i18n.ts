import { register, init, getLocaleFromNavigator, locale, t as originalT } from 'svelte-i18n';
import { browser } from '$app/environment';
import type { TranslationKey } from './types/i18n';
import type { Readable } from 'svelte/store';

register('en', () => import('./locales/en.json'));
register('cs', () => import('./locales/cs.json'));

export const defaultLocale = 'en';
export const supportedLocales = ['en', 'cs'];

export { locale, date, time, number } from 'svelte-i18n';

export function initI18n(serverLocale?: string) {
	if (!browser) {
		init({ fallbackLocale: defaultLocale, initialLocale: serverLocale || defaultLocale });
		return;
	}

	const browserLocale = getLocaleFromNavigator();
	let initialLocale = serverLocale;

	if (!initialLocale) {
		const foundLocale = supportedLocales.find((l) => browserLocale?.startsWith(l));
		initialLocale = foundLocale || defaultLocale;
	}

	init({
		fallbackLocale: defaultLocale,
		initialLocale: initialLocale
	});
}

export function setLanguage(newLocale: string) {
	locale.set(newLocale);
	if (browser) {
		document.cookie = `locale=${newLocale}; path=/; max-age=31536000; SameSite=Lax`;
		document.documentElement.setAttribute('lang', newLocale);
	}
}

export const t = originalT as unknown as Readable<
	(key: TranslationKey, vars?: Record<string, unknown>) => string
>;
