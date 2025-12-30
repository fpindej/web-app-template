/**
 * API error handling utilities for ASP.NET Core backends.
 *
 * Provides type-safe parsing and mapping of validation errors
 * from ASP.NET Core's ProblemDetails format.
 */

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
 * const errors = { PhoneNumber: ["Invalid format"] };
 * const mapped = mapFieldErrors(errors);
 * // Result: { phoneNumber: "Invalid format" }
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
		mapped[fieldName] = messages[0] ?? '';
	}

	return mapped;
}

/**
 * Extracts a user-friendly error message from an API error response.
 *
 * @param error - The error object from the API response
 * @param fallback - Fallback message if no error message can be extracted
 * @returns A user-friendly error message
 */
export function getErrorMessage(error: unknown, fallback: string): string {
	if (typeof error === 'object' && error !== null) {
		const problemDetails = error as ValidationProblemDetails;
		return problemDetails.detail || problemDetails.title || fallback;
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
