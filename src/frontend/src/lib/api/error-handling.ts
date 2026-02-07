/**
 * API error handling utilities for ASP.NET Core backends.
 *
 * Provides type-safe parsing and mapping of validation errors
 * from ASP.NET Core's ProblemDetails format. Error messages from the backend
 * are structured error codes (e.g. "validation.required") that are resolved
 * to localized strings via paraglide-js.
 *
 * @remarks Pattern documented in src/frontend/AGENTS.md — update both when changing.
 */

import * as m from '$lib/paraglide/messages';

/**
 * Extended ProblemDetails with validation errors.
 * ASP.NET Core returns field-level errors in an `errors` object.
 *
 * @see https://tools.ietf.org/html/rfc7807
 */
export interface ValidationProblemDetails {
	type?: string | null;
	title?: string | null;
	status?: number | null;
	detail?: string | null;
	instance?: string | null;
	errors?: Record<string, string[]>;
}

/**
 * Type guard to check if an error response is a ValidationProblemDetails.
 */
export function isValidationProblemDetails(
	error: unknown
): error is ValidationProblemDetails & { errors: Record<string, string[]> } {
	return (
		typeof error === 'object' &&
		error !== null &&
		'errors' in error &&
		typeof (error as ValidationProblemDetails).errors === 'object'
	);
}

/**
 * Static lookup map from backend error codes to paraglide message functions.
 * Each key is a dot-separated error code (e.g. "validation.required") and the value
 * is the corresponding paraglide message function that returns a localized string.
 *
 * This approach preserves type safety and tree-shaking — no dynamic m[key] access.
 * When adding new error codes to ErrorCodes.cs, add a corresponding entry here
 * and in both en.json / cs.json.
 */
const ERROR_CODE_MAP: Record<string, () => string> = {
	// Validation
	'validation.required': m.errorCode_validation_required,
	'validation.invalidEmail': m.errorCode_validation_invalidEmail,
	'validation.maxLength': m.errorCode_validation_maxLength,
	'validation.minLength': m.errorCode_validation_minLength,
	'validation.invalidUrl': m.errorCode_validation_invalidUrl,
	'validation.invalidPhoneNumber': m.errorCode_validation_invalidPhoneNumber,

	// Identity
	'identity.duplicateEmail': m.errorCode_identity_duplicateEmail,
	'identity.duplicateUserName': m.errorCode_identity_duplicateUserName,
	'identity.invalidEmail': m.errorCode_identity_invalidEmail,
	'identity.invalidUserName': m.errorCode_identity_invalidUserName,
	'identity.passwordRequiresDigit': m.errorCode_identity_passwordRequiresDigit,
	'identity.passwordRequiresLower': m.errorCode_identity_passwordRequiresLower,
	'identity.passwordRequiresUpper': m.errorCode_identity_passwordRequiresUpper,
	'identity.passwordRequiresNonAlphanumeric': m.errorCode_identity_passwordRequiresNonAlphanumeric,
	'identity.passwordRequiresUniqueChars': m.errorCode_identity_passwordRequiresUniqueChars,
	'identity.passwordTooShort': m.errorCode_identity_passwordTooShort,
	'identity.userAlreadyInRole': m.errorCode_identity_userAlreadyInRole,
	'identity.userNotInRole': m.errorCode_identity_userNotInRole,
	'identity.userLockout': m.errorCode_identity_userLockout,
	'identity.concurrencyFailure': m.errorCode_identity_concurrencyFailure,
	'identity.defaultError': m.errorCode_identity_defaultError,

	// Auth
	'auth.invalidCredentials': m.errorCode_auth_invalidCredentials,
	'auth.refreshTokenMissing': m.errorCode_auth_refreshTokenMissing,
	'auth.refreshTokenNotFound': m.errorCode_auth_refreshTokenNotFound,
	'auth.refreshTokenInvalidated': m.errorCode_auth_refreshTokenInvalidated,
	'auth.refreshTokenReused': m.errorCode_auth_refreshTokenReused,
	'auth.refreshTokenExpired': m.errorCode_auth_refreshTokenExpired,
	'auth.userNotFound': m.errorCode_auth_userNotFound,

	// User
	'user.notAuthenticated': m.errorCode_user_notAuthenticated,
	'user.notFound': m.errorCode_user_notFound
};

/**
 * Resolves a backend error code to a localized string via paraglide-js.
 * If the code is not recognized, returns the raw code as-is.
 *
 * @param code - A dot-separated error code from the backend (e.g. "validation.required")
 * @returns The localized error message, or the raw code if unrecognized
 */
export function resolveErrorCode(code: string): string {
	const messageFn = ERROR_CODE_MAP[code.trim()];
	return messageFn ? messageFn() : code;
}

/**
 * Resolves a string that may contain comma-separated error codes into
 * an array of localized error messages.
 *
 * The backend's `ErrorResponse.message` field may contain multiple error codes
 * joined by ", " (e.g. "identity.duplicateEmail, identity.passwordRequiresDigit").
 *
 * @param message - A potentially comma-separated string of error codes
 * @returns An array of localized error messages
 */
export function resolveErrorCodes(message: string): string[] {
	return message
		.split(',')
		.map((code) => resolveErrorCode(code.trim()))
		.filter(Boolean);
}

/**
 * Default mapping of PascalCase backend field names to camelCase frontend field names.
 * Extend this map as needed for your application.
 */
const DEFAULT_FIELD_MAP: Record<string, string> = {
	FirstName: 'firstName',
	LastName: 'lastName',
	PhoneNumber: 'phoneNumber',
	Bio: 'bio',
	AvatarUrl: 'avatarUrl',
	Email: 'email',
	Password: 'password',
	ConfirmPassword: 'confirmPassword'
};

/**
 * Maps backend field names (PascalCase) to frontend field names (camelCase).
 *
 * @param errors - The errors object from ValidationProblemDetails
 * @param customFieldMap - Optional custom field name mapping to override defaults
 * @returns A record of field names to their first error message
 *
 * @example
 * ```ts
 * const errors = { PhoneNumber: ["validation.invalidPhoneNumber"] };
 * const mapped = mapFieldErrors(errors);
 * // Result: { phoneNumber: "Please enter a valid phone number." }  (localized)
 * ```
 */
export function mapFieldErrors(
	errors: Record<string, string[]>,
	customFieldMap?: Record<string, string>
): Record<string, string> {
	const fieldMap = { ...DEFAULT_FIELD_MAP, ...customFieldMap };
	const mapped: Record<string, string> = {};

	for (const [key, messages] of Object.entries(errors)) {
		// Use custom mapping, fall back to default, then to lowercase
		const fieldName = fieldMap[key] ?? key.charAt(0).toLowerCase() + key.slice(1);
		const rawMessage = messages[0] ?? '';
		mapped[fieldName] = resolveErrorCode(rawMessage);
	}

	return mapped;
}

/**
 * Extracts a user-friendly error message from an API error response.
 * Supports both ASP.NET Core's ProblemDetails format (detail/title fields)
 * and the custom ErrorResponse format (message field).
 * Resolves error codes to localized strings via paraglide-js.
 *
 * @param error - The error object from the API response
 * @param fallback - Fallback message if no error message can be extracted
 * @returns A user-friendly localized error message
 */
export function getErrorMessage(error: unknown, fallback: string): string {
	if (typeof error === 'object' && error !== null) {
		const err = error as Record<string, unknown>;
		const raw =
			(typeof err.detail === 'string' && err.detail) ||
			(typeof err.title === 'string' && err.title) ||
			(typeof err.message === 'string' && err.message);
		if (raw) {
			// If the message contains comma-separated error codes, resolve all of them
			const resolved = resolveErrorCodes(raw);
			return resolved.length > 0 ? resolved.join(' ') : fallback;
		}
	}
	return fallback;
}

/**
 * Represents a fetch error with a typed cause containing the error code.
 * Node.js fetch errors (and some browser implementations) include a `cause`
 * property with additional error details.
 */
export interface FetchErrorCause {
	code?: string;
	errno?: number;
	syscall?: string;
	hostname?: string;
	message?: string;
}

/**
 * Type guard to check if an error has a fetch error cause with a code.
 * Useful for detecting network errors like ECONNREFUSED, ETIMEDOUT, etc.
 *
 * @example
 * ```ts
 * try {
 *   await fetch(url);
 * } catch (err) {
 *   if (isFetchErrorWithCode(err, 'ECONNREFUSED')) {
 *     return new Response('Backend unavailable', { status: 503 });
 *   }
 * }
 * ```
 */
export function isFetchErrorWithCode(error: unknown, code: string): boolean {
	if (typeof error !== 'object' || error === null) return false;
	const cause = (error as { cause?: FetchErrorCause }).cause;
	return cause?.code === code;
}

/**
 * Extracts the error code from a fetch error's cause, if present.
 *
 * @returns The error code string, or undefined if not a fetch error with cause
 */
export function getFetchErrorCode(error: unknown): string | undefined {
	if (typeof error !== 'object' || error === null) return undefined;
	const cause = (error as { cause?: FetchErrorCause }).cause;
	return cause?.code;
}
