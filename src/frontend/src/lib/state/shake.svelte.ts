import { SvelteSet } from 'svelte/reactivity';

/**
 * Creates a shake state that can be used to trigger shake animations on error.
 *
 * Usage:
 * ```svelte
 * <script>
 *   import { createShake } from '$lib/state';
 *   const shake = createShake();
 *
 *   function handleError() {
 *     shake.trigger();
 *   }
 * </script>
 *
 * <div class={shake.class}>...</div>
 * ```
 */
export function createShake(duration = 500) {
	let isShaking = $state(false);

	return {
		get active() {
			return isShaking;
		},
		get class() {
			return isShaking ? 'animate-shake' : '';
		},
		trigger() {
			isShaking = true;
			setTimeout(() => {
				isShaking = false;
			}, duration);
		}
	};
}

/**
 * Creates a field-level shake state manager for triggering shake animations on specific fields.
 *
 * Usage:
 * ```svelte
 * <script>
 *   import { createFieldShakes } from '$lib/state';
 *   const fieldShakes = createFieldShakes();
 *
 *   function handleValidationError(errors: Record<string, string>) {
 *     fieldShakes.triggerFields(Object.keys(errors));
 *   }
 * </script>
 *
 * <div class={fieldShakes.class('phoneNumber')}>...</div>
 * ```
 */
export function createFieldShakes(duration = 500) {
	const activeFields = new SvelteSet<string>();

	return {
		/**
		 * Check if a specific field is currently shaking.
		 */
		isActive(field: string) {
			return activeFields.has(field);
		},
		/**
		 * Get the shake class for a field (returns 'animate-shake' if active, empty string otherwise).
		 */
		class(field: string) {
			return activeFields.has(field) ? 'animate-shake' : '';
		},
		/**
		 * Trigger shake animation on a single field.
		 */
		trigger(field: string) {
			activeFields.add(field);
			setTimeout(() => {
				activeFields.delete(field);
			}, duration);
		},
		/**
		 * Trigger shake animation on multiple fields at once.
		 */
		triggerFields(fields: string[]) {
			fields.forEach((f) => activeFields.add(f));
			setTimeout(() => {
				fields.forEach((f) => activeFields.delete(f));
			}, duration);
		}
	};
}
