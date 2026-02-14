<script lang="ts">
	import * as Dialog from '$lib/components/ui/dialog';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import * as m from '$lib/paraglide/messages';
	import {
		browserClient,
		getErrorMessage,
		isValidationProblemDetails,
		mapFieldErrors,
		isRateLimited
	} from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { createFieldShakes } from '$lib/state';

	interface Props {
		open: boolean;
	}

	let { open = $bindable() }: Props = $props();

	let password = $state('');
	let isLoading = $state(false);
	let fieldErrors = $state<Record<string, string>>({});
	let generalError = $state('');
	const fieldShakes = createFieldShakes();

	$effect(() => {
		if (open) {
			password = '';
			fieldErrors = {};
			generalError = '';
		}
	});

	async function handleSubmit() {
		fieldErrors = {};
		generalError = '';
		isLoading = true;

		try {
			const { response, error: apiError } = await browserClient.DELETE('/api/users/me', {
				body: { password }
			});

			if (response.ok) {
				open = false;
				toast.success(m.settings_deleteAccount_success());
				await goto(resolve('/login'));
				return;
			}

			if (isRateLimited(response)) {
				toast.error(m.error_rateLimited(), { description: m.error_rateLimitedDescription() });
			} else if (isValidationProblemDetails(apiError)) {
				fieldErrors = mapFieldErrors(apiError.errors);
				fieldShakes.triggerFields(Object.keys(fieldErrors));
			} else {
				generalError = getErrorMessage(apiError, m.settings_deleteAccount_error());
				fieldShakes.trigger('password');
			}
		} catch {
			toast.error(m.settings_deleteAccount_error());
		} finally {
			isLoading = false;
		}
	}
</script>

<Dialog.Root bind:open>
	<Dialog.Content class="sm:max-w-md">
		<Dialog.Header>
			<Dialog.Title>{m.settings_deleteAccount_dialogTitle()}</Dialog.Title>
			<Dialog.Description>
				{m.settings_deleteAccount_dialogDescription()}
			</Dialog.Description>
		</Dialog.Header>
		<form
			onsubmit={(e) => {
				e.preventDefault();
				handleSubmit();
			}}
		>
			<div class="grid gap-4 py-4">
				<div class="grid gap-2">
					<Label for="deleteAccountPassword">{m.settings_deleteAccount_password()}</Label>
					<Input
						id="deleteAccountPassword"
						type="password"
						autocomplete="current-password"
						bind:value={password}
						placeholder={m.settings_deleteAccount_passwordPlaceholder()}
						aria-invalid={!!fieldErrors.password || !!generalError}
						aria-describedby={fieldErrors.password || generalError
							? 'deleteAccountPasswordError'
							: undefined}
						class={fieldShakes.class('password')}
						disabled={isLoading}
					/>
					{#if fieldErrors.password}
						<p id="deleteAccountPasswordError" class="text-xs text-destructive">
							{fieldErrors.password}
						</p>
					{:else if generalError}
						<p id="deleteAccountPasswordError" class="text-xs text-destructive">
							{generalError}
						</p>
					{/if}
				</div>
			</div>
			<Dialog.Footer class="flex-col-reverse gap-2 sm:flex-row">
				<Dialog.Close>
					{#snippet child({ props })}
						<Button {...props} variant="outline" disabled={isLoading}>
							{m.settings_deleteAccount_cancel()}
						</Button>
					{/snippet}
				</Dialog.Close>
				<Button type="submit" variant="destructive" disabled={isLoading || !password}>
					{m.settings_deleteAccount_confirm()}
				</Button>
			</Dialog.Footer>
		</form>
	</Dialog.Content>
</Dialog.Root>
