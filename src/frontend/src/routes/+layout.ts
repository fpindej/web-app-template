import { setLocale, locales, baseLocale } from '$lib/paraglide/runtime';
import type { LayoutLoad } from './$types';

export const load: LayoutLoad = async ({ data }) => {
	const locale = data.locale;
	if (locales.includes(locale as (typeof locales)[number])) {
		setLocale(locale as (typeof locales)[number]);
	} else {
		setLocale(baseLocale);
	}
	return data;
};
