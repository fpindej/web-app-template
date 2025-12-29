import type { locales } from '$lib/paraglide/runtime';

type AvailableLanguageTag = (typeof locales)[number];

/**
 * Language metadata configuration for the application.
 * Maps locale codes to their display labels and flag icons.
 *
 * Flags use the `flag-icons` CSS library (fi-{code} class).
 * @see https://flagicons.lipis.dev/
 */
export const LANGUAGE_METADATA: Record<AvailableLanguageTag, { label: string; flag: string }> = {
	en: { label: 'English', flag: 'gb' },
	cs: { label: 'Čeština', flag: 'cz' }
};
