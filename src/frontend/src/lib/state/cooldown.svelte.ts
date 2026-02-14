/**
 * Creates a reactive rate-limit cooldown timer.
 *
 * After a 429 response, call `start(seconds)` with the value from
 * `getRetryAfterSeconds()`. While active, `active` is true and
 * `remaining` counts down every second — use `active` to disable
 * submit buttons.
 *
 * @example
 * ```svelte
 * <script>
 *   import { createCooldown } from '$lib/state';
 *   const cooldown = createCooldown();
 *
 *   function handleSubmit() {
 *     if (isRateLimited(response)) {
 *       cooldown.start(getRetryAfterSeconds(response) ?? 60);
 *       return;
 *     }
 *   }
 * </script>
 *
 * <button disabled={cooldown.active}>
 *   {cooldown.active ? `Wait ${cooldown.remaining}s` : 'Submit'}
 * </button>
 * ```
 */
export function createCooldown() {
	let remaining = $state(0);
	let intervalId: ReturnType<typeof setInterval> | null = null;

	function clear() {
		if (intervalId) {
			clearInterval(intervalId);
			intervalId = null;
		}
		remaining = 0;
	}

	return {
		/** Whether the cooldown is currently active. */
		get active() {
			return remaining > 0;
		},
		/** Seconds remaining until the cooldown expires. */
		get remaining() {
			return remaining;
		},
		/**
		 * Starts (or restarts) the cooldown for the given number of seconds.
		 * Automatically clears when the countdown reaches zero.
		 */
		start(seconds: number) {
			clear();
			remaining = seconds;
			intervalId = setInterval(() => {
				remaining--;
				if (remaining <= 0) {
					clear();
				}
			}, 1000);
		}
	};
}
