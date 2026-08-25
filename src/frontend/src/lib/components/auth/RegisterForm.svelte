<script lang="ts">
	import { browserClient, getErrorMessage, handleMutationError } from '$lib/api';
	import { cn } from '$lib/utils';
	import { createShake, createCooldown } from '$lib/state';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { routes } from '$lib/config';
	import { onMount } from 'svelte';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import * as Form from '$lib/components/ui/form';
	import { PhoneInput } from '$lib/components/ui/phone-input';
	import { AuthShell, TurnstileWidget } from '$lib/components/auth';
	import { OAuthProviderButtons } from '$lib/components/oauth';
	import * as m from '$lib/paraglide/messages';
	import { toast } from '$lib/components/ui/sonner';
	import { Loader2 } from '@lucide/svelte';
	import { registerSchema } from '$lib/schemas/auth';
	import { superForm, defaults, setError } from 'sveltekit-superforms';
	import { zod4 as zod } from 'sveltekit-superforms/adapters';

	interface Props {
		turnstileSiteKey: string;
	}

	let { turnstileSiteKey }: Props = $props();

	const shake = createShake();
	const cooldown = createCooldown();
	let captchaToken = $state('');
	let resetCaptcha: (() => void) | null = $state(null);

	const DRAFT_KEY = 'register-form-draft';

	const superform = superForm(defaults(zod(registerSchema)), {
		SPA: true,
		validators: zod(registerSchema),
		async onUpdate({ form }) {
			if (!form.valid || cooldown.active) return;

			try {
				const { response, error: apiError } = await browserClient.POST('/api/auth/register', {
					body: {
						email: form.data.email,
						password: form.data.password,
						captchaToken,
						firstName: form.data.firstName || undefined,
						lastName: form.data.lastName || undefined,
						phoneNumber: form.data.phoneNumber || undefined
					}
				});

				if (response.ok) {
					toast.success(m.auth_register_success());
					clearDraft();
					const loginUrl = `${resolve(routes.login)}?email=${encodeURIComponent(form.data.email)}`;
					// eslint-disable-next-line svelte/no-navigation-without-resolve -- path is resolved above, query string appended
					await goto(loginUrl);
				} else {
					handleMutationError(response, apiError, {
						cooldown,
						fallback: m.auth_register_failed(),
						onValidationError(errors) {
							for (const [field, msg] of Object.entries(errors)) {
								setError(form, field as never, msg);
							}
						},
						onError() {
							toast.error(m.auth_register_failed(), {
								description: getErrorMessage(apiError, m.auth_register_failed())
							});
							shake.trigger();
						}
					});
					resetCaptcha?.();
					captchaToken = '';
				}
			} catch {
				toast.error(m.auth_register_failed());
				shake.trigger();
				resetCaptcha?.();
				captchaToken = '';
			}
		}
	});

	const { form: formData, submitting, enhance } = superform;

	function saveDraft() {
		try {
			localStorage.setItem(
				DRAFT_KEY,
				JSON.stringify({
					firstName: $formData.firstName,
					lastName: $formData.lastName,
					email: $formData.email,
					phoneNumber: $formData.phoneNumber
				})
			);
		} catch {
			/* localStorage may be unavailable */
		}
	}

	function loadDraft() {
		try {
			const raw = localStorage.getItem(DRAFT_KEY);
			if (!raw) return;
			const draft = JSON.parse(raw);
			$formData.firstName = draft.firstName ?? '';
			$formData.lastName = draft.lastName ?? '';
			$formData.email = draft.email ?? '';
			$formData.phoneNumber = draft.phoneNumber ?? '';
		} catch {
			/* ignore parse errors */
		}
	}

	function clearDraft() {
		try {
			localStorage.removeItem(DRAFT_KEY);
		} catch {
			/* localStorage may be unavailable */
		}
	}

	onMount(() => {
		loadDraft();
	});

	$effect(() => {
		saveDraft();
	});
</script>

<AuthShell cardClass={cn(shake.active && 'animate-shake border-destructive')}>
	<div class="flex flex-col gap-6">
		<div class="flex flex-col items-center gap-2 text-center">
			<h1 class="text-2xl font-bold tracking-tight">{m.auth_register_title()}</h1>
			<p class="text-sm text-balance text-muted-foreground">
				{m.auth_register_description()}
			</p>
		</div>

		<form method="POST" use:enhance class="grid gap-4">
			<div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
				<Form.Field form={superform} name="firstName">
					<Form.Control>
						{#snippet children({ props })}
							<Form.Label>{m.auth_register_firstName()}</Form.Label>
							<Input
								{...props}
								autocomplete="given-name"
								bind:value={$formData.firstName}
								disabled={$submitting}
							/>
						{/snippet}
					</Form.Control>
					<Form.FieldErrors />
				</Form.Field>

				<Form.Field form={superform} name="lastName">
					<Form.Control>
						{#snippet children({ props })}
							<Form.Label>{m.auth_register_lastName()}</Form.Label>
							<Input
								{...props}
								autocomplete="family-name"
								bind:value={$formData.lastName}
								disabled={$submitting}
							/>
						{/snippet}
					</Form.Control>
					<Form.FieldErrors />
				</Form.Field>
			</div>

			<Form.Field form={superform} name="email">
				<Form.Control>
					{#snippet children({ props })}
						<Form.Label>{m.auth_register_email()}</Form.Label>
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

			<Form.Field form={superform} name="phoneNumber">
				<Form.Control>
					{#snippet children({ props })}
						<Form.Label>{m.auth_register_phone()}</Form.Label>
						{@const { 'aria-invalid': ariaInvalid, ...phoneProps } = props}
						<PhoneInput
							{...phoneProps}
							aria-invalid={ariaInvalid === 'true'}
							bind:value={$formData.phoneNumber}
							disabled={$submitting}
						/>
					{/snippet}
				</Form.Control>
				<Form.FieldErrors />
			</Form.Field>

			<Form.Field form={superform} name="password">
				<Form.Control>
					{#snippet children({ props })}
						<Form.Label>{m.auth_register_password()}</Form.Label>
						<Input
							{...props}
							type="password"
							autocomplete="new-password"
							required
							minlength={6}
							bind:value={$formData.password}
							disabled={$submitting}
						/>
					{/snippet}
				</Form.Control>
				<Form.FieldErrors />
			</Form.Field>

			<Form.Field form={superform} name="confirmPassword">
				<Form.Control>
					{#snippet children({ props })}
						<Form.Label>{m.auth_register_confirmPassword()}</Form.Label>
						<Input
							{...props}
							type="password"
							autocomplete="new-password"
							required
							minlength={6}
							bind:value={$formData.confirmPassword}
							disabled={$submitting}
						/>
					{/snippet}
				</Form.Control>
				<Form.FieldErrors />
			</Form.Field>

			<TurnstileWidget
				siteKey={turnstileSiteKey}
				onVerified={(t) => (captchaToken = t)}
				onError={() => toast.error(m.auth_captcha_error())}
				resetRef={(fn) => (resetCaptcha = fn)}
			/>

			<Button
				type="submit"
				class="w-full"
				disabled={$submitting || cooldown.active || !captchaToken}
			>
				{#if cooldown.active}
					{m.common_waitSeconds({ seconds: cooldown.remaining })}
				{:else}
					{#if $submitting}
						<Loader2 class="me-2 h-4 w-4 animate-spin" />
					{/if}
					{m.auth_register_submit()}
				{/if}
			</Button>
		</form>

		<OAuthProviderButtons />

		<div class="text-center text-sm">
			<span class="text-muted-foreground">{m.auth_register_haveAccount()}</span>
			<a
				href={resolve(routes.login)}
				class="ms-1 inline-flex min-h-11 items-center font-medium text-primary hover:underline"
			>
				{m.auth_register_signIn()}
			</a>
		</div>
	</div>
</AuthShell>
