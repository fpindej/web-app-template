import { initI18n } from '$lib/i18n';
import { waitLocale } from 'svelte-i18n';
import type { LayoutLoad } from './$types';

export const load: LayoutLoad = async ({ data }) => {
	await initI18n(data.locale);
	await waitLocale();
	return data;
};
