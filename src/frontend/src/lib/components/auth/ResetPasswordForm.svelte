<script lang="ts">
	import { browserClient, getErrorMessage, handleMutationError } from '$lib/api';
	import { cn } from '$lib/utils';
	import { createFieldShakes, createCooldown } from '$lib/state';
	import { invalidateAll } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { routes } from '$lib/config';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { FieldError, IconCircle } from '$lib/components/common';
	import { Label } from '$lib/components/ui/label';
	import * as m from '$lib/paraglide/messages';
	import { Check, CircleAlert, TriangleAlert } from '@lucide/svelte';
	import { AuthShell } from '$lib/components/auth';
	import type { User } from '$lib/types';

	interface Props {
		token: string;
		invited?: boolean;
		user?: User | null;
	}

	let { token, invited = false, user = null }: Props = $props();

	let newPassword = $state('');
	let confirmPassword = $state('');
	let isLoading = $state(false);
	let isSuccess = $state(false);
	let isError = $state(false);
	let errorMessage = $state('');
	let fieldErrors = $state<Record<string, string>>({});
	const fieldShakes = createFieldShakes();
	const cooldown = createCooldown();

	let isSigningOut = $state(false);
	let isMissingParams = $derived(!token);

	async function signOutAndContinue() {
		isSigningOut = true;
		try {
			await browserClient.POST('/api/auth/logout');
		} catch {
			// Tokens may already be expired - that's fine
		}
		await invalidateAll();
		isSigningOut = false;
	}

	async function submit(e: Event) {
		e.preventDefault();
		if (isLoading || cooldown.active) return;

		fieldErrors = {};

		if (newPassword !== confirmPassword) {
			fieldErrors = { confirmPassword: m.auth_resetPassword_mismatch() };
			fieldShakes.triggerFields(['confirmPassword']);
			return;
		}

		isLoading = true;

		try {
			const { response, error: apiError } = await browserClient.POST('/api/auth/password/reset', {
				body: { token, newPassword }
			});

			if (response.ok) {
				isSuccess = true;
			} else {
				handleMutationError(response, apiError, {
					cooldown,
					fallback: invited ? m.auth_setPassword_error() : m.auth_resetPassword_error(),
					onValidationError(errors) {
						fieldErrors = errors;
						fieldShakes.triggerFields(Object.keys(errors));
					},
					onError() {
						errorMessage = getErrorMessage(
							apiError,
							invited ? m.auth_setPassword_error() : m.auth_resetPassword_error()
						);
						isError = true;
					}
				});
			}
		} catch {
			errorMessage = invited ? m.auth_setPassword_error() : m.auth_resetPassword_error();
			isError = true;
		} finally {
			isLoading = false;
		}
	}
</script>

<AuthShell>
	{#if user}
		<div class="flex flex-col items-center gap-4 py-4">
			<IconCircle icon={TriangleAlert} variant="warning" />
			<div class="flex flex-col items-center gap-2 text-center">
				<h1 class="text-2xl font-bold tracking-tight">
					{m.auth_resetPassword_alreadySignedInTitle()}
				</h1>
				<p class="text-sm text-balance text-muted-foreground">
					{invited
						? m.auth_resetPassword_alreadySignedInInvitedDescription({
								email: user.email ?? ''
							})
						: m.auth_resetPassword_alreadySignedInDescription({
								email: user.email ?? ''
							})}
				</p>
			</div>
			<div class="flex w-full flex-col gap-2">
				<Button class="w-full" disabled={isSigningOut} onclick={signOutAndContinue}>
					{isSigningOut
						? m.auth_resetPassword_signingOut()
						: m.auth_resetPassword_signOutAndContinue()}
				</Button>
				<a href={resolve(routes.dashboard)} class="block">
					<Button variant="outline" class="w-full">
						{m.auth_resetPassword_goToDashboard()}
					</Button>
				</a>
			</div>
		</div>
	{:else if isMissingParams}
		<div class="flex flex-col items-center gap-4 py-4">
			<IconCircle icon={CircleAlert} variant="error" />
			<div class="flex flex-col items-center gap-2 text-center">
				<h1 class="text-2xl font-bold tracking-tight">
					{m.auth_resetPassword_invalidLink()}
				</h1>
				<p class="text-sm text-balance text-muted-foreground">
					{m.auth_resetPassword_invalidLinkDescription()}
				</p>
			</div>
			<a
				href={resolve(routes.forgotPassword)}
				class="inline-flex min-h-11 items-center text-sm font-medium text-primary hover:underline"
			>
				{m.auth_resetPassword_requestNew()}
			</a>
		</div>
	{:else if isError}
		<div class="flex flex-col items-center gap-4 py-4">
			<IconCircle icon={CircleAlert} variant="error" />
			<div class="flex flex-col items-center gap-2 text-center">
				<h1 class="text-2xl font-bold tracking-tight">
					{m.auth_resetPassword_errorTitle()}
				</h1>
				<p class="text-sm text-balance text-muted-foreground">
					{errorMessage}
				</p>
			</div>
			<div class="text-center text-sm">
				{#if invited}
					<p class="text-muted-foreground">
						{m.auth_resetPassword_errorContactAdmin()}
					</p>
				{:else}
					<a
						href={resolve(routes.forgotPassword)}
						class="inline-flex min-h-11 items-center font-medium text-primary hover:underline"
					>
						{m.auth_resetPassword_requestNew()}
					</a>
				{/if}
			</div>
		</div>
	{:else if !isSuccess}
		<div class="flex flex-col gap-6">
			<div class="flex flex-col items-center gap-2 text-center">
				<h1 class="text-2xl font-bold tracking-tight">
					{invited ? m.auth_setPassword_title() : m.auth_resetPassword_title()}
				</h1>
				<p class="text-sm text-balance text-muted-foreground">
					{invited ? m.auth_setPassword_subtitle() : m.auth_resetPassword_subtitle()}
				</p>
			</div>

			<form class="space-y-6" onsubmit={submit}>
				<div class="grid gap-2">
					<Label for="newPassword">{m.auth_resetPassword_newPassword()}</Label>
					<Input
						id="newPassword"
						type="password"
						autocomplete="new-password"
						required
						minlength={6}
						bind:value={newPassword}
						class={cn(fieldShakes.class('newPassword'))}
						aria-invalid={!!fieldErrors.newPassword}
						aria-describedby={fieldErrors.newPassword ? 'newPassword-error' : undefined}
					/>
					<FieldError id="newPassword-error" message={fieldErrors.newPassword} />
				</div>

				<div class="grid gap-2">
					<Label for="confirmPassword">{m.auth_resetPassword_confirmPassword()}</Label>
					<Input
						id="confirmPassword"
						type="password"
						autocomplete="new-password"
						required
						bind:value={confirmPassword}
						class={cn(fieldShakes.class('confirmPassword'))}
						aria-invalid={!!fieldErrors.confirmPassword}
						aria-describedby={fieldErrors.confirmPassword ? 'confirmPassword-error' : undefined}
					/>
					<FieldError id="confirmPassword-error" message={fieldErrors.confirmPassword} />
				</div>

				<Button type="submit" class="w-full" disabled={isLoading || cooldown.active}>
					{#if cooldown.active}
						{m.common_waitSeconds({ seconds: cooldown.remaining })}
					{:else if isLoading}
						{invited ? m.auth_setPassword_submitting() : m.auth_resetPassword_submitting()}
					{:else}
						{invited ? m.auth_setPassword_submit() : m.auth_resetPassword_submit()}
					{/if}
				</Button>
			</form>
		</div>
	{:else}
		<div class="flex flex-col items-center gap-4 py-4">
			<IconCircle icon={Check} variant="success" />
			<div class="flex flex-col items-center gap-2 text-center">
				<h1 class="text-2xl font-bold tracking-tight">
					{invited ? m.auth_setPassword_successTitle() : m.auth_resetPassword_successTitle()}
				</h1>
				<p class="text-sm text-balance text-muted-foreground">
					{invited
						? m.auth_setPassword_successDescription()
						: m.auth_resetPassword_successDescription()}
				</p>
			</div>
			<a href={resolve(routes.login)} class="block">
				<Button class="w-full">{m.auth_resetPassword_signIn()}</Button>
			</a>
		</div>
	{/if}
</AuthShell>
