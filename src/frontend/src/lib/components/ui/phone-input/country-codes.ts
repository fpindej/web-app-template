/**
 * Country code configuration for phone number input.
 * Uses flag-icons CSS library for flag display.
 *
 * @see https://flagicons.lipis.dev/
 */

export interface CountryCode {
	/** ISO 3166-1 alpha-2 country code (for flag and translation key) */
	code: string;
	/** International dialing code (with +) */
	dialCode: string;
}

/**
 * Common European and international country codes.
 * Ordered by likely usage frequency for European users.
 * Country names are localized via the `country_{code}` translation keys.
 */
export const COUNTRY_CODES: CountryCode[] = [
	{ code: 'cz', dialCode: '+420' },
	{ code: 'sk', dialCode: '+421' },
	{ code: 'de', dialCode: '+49' },
	{ code: 'at', dialCode: '+43' },
	{ code: 'pl', dialCode: '+48' },
	{ code: 'gb', dialCode: '+44' },
	{ code: 'us', dialCode: '+1' },
	{ code: 'fr', dialCode: '+33' },
	{ code: 'it', dialCode: '+39' },
	{ code: 'es', dialCode: '+34' },
	{ code: 'nl', dialCode: '+31' },
	{ code: 'be', dialCode: '+32' },
	{ code: 'ch', dialCode: '+41' },
	{ code: 'hu', dialCode: '+36' },
	{ code: 'ro', dialCode: '+40' },
	{ code: 'ua', dialCode: '+380' }
];

/**
 * Finds a country code entry by dial code.
 */
export function findCountryByDialCode(dialCode: string): CountryCode | undefined {
	return COUNTRY_CODES.find((c) => c.dialCode === dialCode);
}

/**
 * Extracts the dial code from a full phone number.
 * Returns the matched country and the remaining number.
 */
export function parsePhoneNumber(phone: string): {
	country: CountryCode | undefined;
	nationalNumber: string;
} {
	const cleaned = phone.trim();

	if (!cleaned.startsWith('+')) {
		return { country: undefined, nationalNumber: cleaned };
	}

	// Try to match longest dial codes first (e.g., +380 before +38)
	const sortedCodes = [...COUNTRY_CODES].sort((a, b) => b.dialCode.length - a.dialCode.length);

	for (const country of sortedCodes) {
		if (cleaned.startsWith(country.dialCode)) {
			return {
				country,
				nationalNumber: cleaned.slice(country.dialCode.length).trim()
			};
		}
	}

	return { country: undefined, nationalNumber: cleaned };
}

/**
 * Formats a phone number with dial code.
 */
export function formatPhoneNumber(dialCode: string, nationalNumber: string): string {
	const cleanedNumber = nationalNumber.trim();
	if (!cleanedNumber) return '';
	return `${dialCode}${cleanedNumber}`;
}
