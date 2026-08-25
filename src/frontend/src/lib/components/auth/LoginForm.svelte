<script lang="ts">
	import { browserClient, getErrorMessage, handleMutationError } from '$lib/api';
	import { cn } from '$lib/utils';
	import { createShake, createCooldown, healthState } from '$lib/state';
	import { goto, invalidateAll } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { routes } from '$lib/config';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Checkbox } from '$lib/components/ui/checkbox';
	import { Label } from '$lib/components/ui/label';
	import * as Form from '$lib/components/ui/form';
	import { StatusIndicator } from '$lib/components/common';
	import * as m from '$lib/paraglide/messages';
	import { Loader2 } from '@lucide/svelte';
	import { AuthShell, TwoFactorStep } from '$lib/components/auth';
	import { OAuthProviderButtons } from '$lib/components/oauth';
	import { toast } from '$lib/components/ui/sonner';
	import { loginSchema } from '$lib/schemas/auth';
	import { superForm, defaults } from 'sveltekit-superforms';
	import { zod4 as zod } from 'sveltekit-superforms/adapters';

	interface Props {
		apiUrl?: string;
		prefillEmail?: string;
	}

	let { apiUrl, prefillEmail }: Props = $props();

	const shake = createShake();
	const cooldown = createCooldown();

	let challengeToken = $state('');
	let requiresTwoFactor = $state(false);
	let loginSuccess = $state(false);

	let isApiOnline = $derived(!healthState.checked || healthState.online);

	const superform = superForm(defaults(zod(loginSchema)), {
		SPA: true,
		validators: zod(loginSchema),
		async onUpdate({ form }) {
			if (!form.valid || cooldown.active) return;

			try {
				const {
					response,
					data,
					error: apiError
				} = await browserClient.POST('/api/auth/login', {
					body: {
						username: form.data.email,
						password: form.data.password,
						rememberMe: form.data.rememberMe
					}
				});

				if (response.ok && data) {
					if (data.requiresTwoFactor && data.challengeToken) {
						challengeToken = data.challengeToken;
						$formData.password = '';
						requiresTwoFactor = true;
					} else {
						await completeLogin();
					}
				} else {
					handleMutationError(response, apiError, {
						cooldown,
						fallback: m.auth_login_error(),
						onRateLimited: () => shake.trigger(),
						onError() {
							const errorMessage = getErrorMessage(apiError, m.auth_login_error(), {
								auth_login_invalid_credentials: m.auth_login_invalidCredentials,
								auth_login_account_locked: m.auth_login_accountLocked
							});
							toast.error(m.auth_login_failed(), { description: errorMessage });
							shake.trigger();
						}
					});
				}
			} catch {
				toast.error(m.auth_login_failed(), { description: m.auth_login_error() });
				shake.trigger();
			}
		}
	});

	const { form: formData, submitting, enhance } = superform;

	let prefillApplied = false;
	$effect(() => {
		if (!prefillApplied && prefillEmail) {
			$formData.email = prefillEmail;
			prefillApplied = true;
		}
	});

	async function completeLogin() {
		loginSuccess = true;
		toast.success(m.auth_login_success());
		const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
		if (!prefersReducedMotion) {
			await new Promise((r) => setTimeout(r, 400));
		}
		await invalidateAll();
		await goto(resolve(routes.dashboard));
	}

	function handleTwoFactorBack() {
		requiresTwoFactor = false;
		challengeToken = '';
	}
</script>

<AuthShell
	cardClass={cn(shake.active && 'animate-shake border-destructive')}
	success={loginSuccess}
>
	{#snippet extras()}
		<div
			class="group absolute start-[max(1rem,env(safe-area-inset-left,0px))] bottom-[max(1rem,env(safe-area-inset-bottom,0px))] flex cursor-default items-center gap-2 rounded-lg px-2 py-1.5 text-xs text-muted-foreground/60 transition-all hover:bg-muted/50 hover:text-muted-foreground"
			title={apiUrl}
		>
			<StatusIndicator status={isApiOnline ? 'online' : 'offline'} size="sm" />
			<span class="hidden group-hover:inline">{apiUrl ?? 'API'}</span>
		</div>
	{/snippet}

	{#if requiresTwoFactor}
		<TwoFactorStep {challengeToken} onSuccess={completeLogin} onBack={handleTwoFactorBack} />
	{:else}
		<div class="flex flex-col gap-6">
			<div class="flex flex-col items-center gap-2 text-center">
				<h1 class="text-2xl font-bold tracking-tight">
					{m.auth_login_title()}
				</h1>
				<p class="text-sm text-balance text-muted-foreground">
					{m.auth_login_subtitle()}
				</p>
			</div>

			<form method="POST" use:enhance class="space-y-6">
				<Form.Field form={superform} name="email">
					<Form.Control>
						{#snippet children({ props })}
							<Form.Label>{m.auth_login_email()}</Form.Label>
							<Input
								{...props}
								type="email"
								autocomplete="email"
								required
								bind:value={$formData.email}
								disabled={$submitting}
							/>
						{/snippet}
					</Form.Control>
					<Form.FieldErrors />
				</Form.Field>

				<div class="grid gap-2">
					<Form.Field form={superform} name="password">
						<Form.Control>
							{#snippet children({ props })}
								<div class="flex items-center justify-between">
									<Form.Label>{m.auth_login_password()}</Form.Label>
									<a
										href={resolve(routes.forgotPassword)}
										class="inline-flex min-h-11 items-center text-sm font-medium text-primary hover:underline"
									>
										{m.auth_login_forgotPassword()}
									</a>
								</div>
								<Input
									{...props}
									type="password"
									autocomplete="current-password"
									required
									bind:value={$formData.password}
									disabled={$submitting}
								/>
							{/snippet}
						</Form.Control>
						<Form.FieldErrors />
					</Form.Field>

					<div class="flex items-center gap-2">
						<Checkbox id="rememberMe" bind:checked={$formData.rememberMe} disabled={$submitting} />
						<Label for="rememberMe" class="text-sm font-normal">
							{m.auth_login_rememberMe()}
						</Label>
					</div>
				</div>

				<Button
					type="submit"
					class="w-full"
					disabled={!isApiOnline || $submitting || cooldown.active}
				>
					{#if cooldown.active}
						{m.common_waitSeconds({ seconds: cooldown.remaining })}
					{:else}
						{#if $submitting}
							<Loader2 class="me-2 h-4 w-4 animate-spin" />
						{/if}
						{isApiOnline ? m.auth_login_submit() : m.auth_login_apiOffline()}
					{/if}
				</Button>
			</form>

			<OAuthProviderButtons />

			<div class="text-center text-sm">
				<span class="text-muted-foreground">{m.auth_login_noAccount()}</span>
				<a
					href={resolve(routes.register)}
					class="ms-1 inline-flex min-h-11 items-center font-medium text-primary hover:underline"
				>
					{m.auth_login_signUp()}
				</a>
			</div>
		</div>
	{/if}
</AuthShell>
