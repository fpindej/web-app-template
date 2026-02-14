<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import * as m from '$lib/paraglide/messages';
	import {
		browserClient,
		isValidationProblemDetails,
		mapFieldErrors,
		getErrorMessage,
		isRateLimited
	} from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { goto, invalidateAll } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { createFieldShakes } from '$lib/state';

	// Form state
	let currentPassword = $state('');
	let newPassword = $state('');
	let confirmPassword = $state('');
	let isLoading = $state(false);

	// Field-level errors from backend validation or client-side checks
	let fieldErrors = $state<Record<string, string>>({});

	// Field-level shake animation for error feedback
	const fieldShakes = createFieldShakes();

	async function handleSubmit(e: Event) {
		e.preventDefault();
		isLoading = true;
		fieldErrors = {};

		// Client-side validation: confirm password must match new password
		if (newPassword !== confirmPassword) {
			fieldErrors = { confirmPassword: m.settings_changePassword_mismatch() };
			fieldShakes.triggerFields(['confirmPassword']);
			isLoading = false;
			return;
		}

		try {
			const { response, error: apiError } = await browserClient.POST('/api/auth/change-password', {
				body: { currentPassword, newPassword }
			});

			if (response.ok) {
				currentPassword = '';
				newPassword = '';
				confirmPassword = '';
				toast.success(m.settings_changePassword_success());
				await invalidateAll();
				await goto(resolve('/login'));
			} else if (isRateLimited(response)) {
				toast.error(m.error_rateLimited(), { description: m.error_rateLimitedDescription() });
			} else if (isValidationProblemDetails(apiError)) {
				fieldErrors = mapFieldErrors(apiError.errors);
				fieldShakes.triggerFields(Object.keys(fieldErrors));
				toast.error(getErrorMessage(apiError, m.settings_changePassword_error()));
			} else {
				const description = getErrorMessage(apiError, '');
				toast.error(m.settings_changePassword_error(), description ? { description } : undefined);
			}
		} catch {
			toast.error(m.settings_changePassword_error());
		} finally {
			isLoading = false;
		}
	}
</script>

<Card.Root class="card-hover">
	<Card.Header>
		<Card.Title>{m.settings_changePassword_title()}</Card.Title>
		<Card.Description>{m.settings_changePassword_description()}</Card.Description>
	</Card.Header>
	<Card.Content>
		<form onsubmit={handleSubmit}>
			<div class="grid gap-4">
				<div class="grid gap-2">
					<Label for="currentPassword">{m.settings_changePassword_currentPassword()}</Label>
					<Input
						id="currentPassword"
						type="password"
						autocomplete="current-password"
						bind:value={currentPassword}
						required
						class={fieldShakes.class('currentPassword')}
						aria-invalid={!!fieldErrors.currentPassword}
						aria-describedby={fieldErrors.currentPassword ? 'currentPassword-error' : undefined}
					/>
					{#if fieldErrors.currentPassword}
						<p id="currentPassword-error" class="text-xs text-destructive">
							{fieldErrors.currentPassword}
						</p>
					{/if}
				</div>

				<div class="grid gap-2">
					<Label for="newPassword">{m.settings_changePassword_newPassword()}</Label>
					<Input
						id="newPassword"
						type="password"
						autocomplete="new-password"
						bind:value={newPassword}
						required
						minlength={6}
						class={fieldShakes.class('newPassword')}
						aria-invalid={!!fieldErrors.newPassword}
						aria-describedby={fieldErrors.newPassword ? 'newPassword-error' : undefined}
					/>
					{#if fieldErrors.newPassword}
						<p id="newPassword-error" class="text-xs text-destructive">
							{fieldErrors.newPassword}
						</p>
					{/if}
				</div>

				<div class="grid gap-2">
					<Label for="confirmPassword">{m.settings_changePassword_confirmPassword()}</Label>
					<Input
						id="confirmPassword"
						type="password"
						autocomplete="new-password"
						bind:value={confirmPassword}
						required
						class={fieldShakes.class('confirmPassword')}
						aria-invalid={!!fieldErrors.confirmPassword}
						aria-describedby={fieldErrors.confirmPassword ? 'confirmPassword-error' : undefined}
					/>
					{#if fieldErrors.confirmPassword}
						<p id="confirmPassword-error" class="text-xs text-destructive">
							{fieldErrors.confirmPassword}
						</p>
					{/if}
				</div>

				<div class="flex justify-end">
					<Button type="submit" disabled={isLoading}>
						{isLoading
							? m.settings_changePassword_submitting()
							: m.settings_changePassword_submit()}
					</Button>
				</div>
			</div>
		</form>
	</Card.Content>
</Card.Root>
