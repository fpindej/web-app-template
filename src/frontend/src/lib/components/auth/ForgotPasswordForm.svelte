<script lang="ts">
	import { browserClient, getErrorMessage, handleMutationError } from '$lib/api';
	import { cn } from '$lib/utils';
	import { createShake, createCooldown } from '$lib/state';
	import { resolve } from '$app/paths';
	import { routes } from '$lib/config';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import * as m from '$lib/paraglide/messages';
	import { MailCheck } from '@lucide/svelte';
	import { IconCircle } from '$lib/components/common';
	import { AuthShell, TurnstileWidget } from '$lib/components/auth';
	import { toast } from '$lib/components/ui/sonner';

	interface Props {
		turnstileSiteKey: string;
	}

	let { turnstileSiteKey }: Props = $props();

	let email = $state('');
	let captchaToken = $state('');
	let resetCaptcha: (() => void) | null = $state(null);
	let isLoading = $state(false);
	let isSubmitted = $state(false);
	const shake = createShake();
	const cooldown = createCooldown();

	async function submit(e: Event) {
		e.preventDefault();
		if (isLoading || cooldown.active) return;

		isLoading = true;

		try {
			const { response, error: apiError } = await browserClient.POST('/api/auth/password/forgot', {
				body: { email, captchaToken }
			});

			if (response.ok) {
				isSubmitted = true;
			} else {
				handleMutationError(response, apiError, {
					cooldown,
					fallback: m.auth_forgotPassword_error(),
					onRateLimited: () => shake.trigger(),
					onError() {
						toast.error(m.auth_forgotPassword_error(), {
							description: getErrorMessage(apiError, m.auth_forgotPassword_error())
						});
						shake.trigger();
					}
				});
				resetCaptcha?.();
				captchaToken = '';
			}
		} catch {
			toast.error(m.auth_forgotPassword_error());
			shake.trigger();
			resetCaptcha?.();
			captchaToken = '';
		} finally {
			isLoading = false;
		}
	}
</script>

<AuthShell cardClass={cn(shake.active && 'animate-shake border-destructive')}>
	{#if !isSubmitted}
		<div class="flex flex-col gap-6">
			<div class="flex flex-col items-center gap-2 text-center">
				<h1 class="text-2xl font-bold tracking-tight">{m.auth_forgotPassword_title()}</h1>
				<p class="text-sm text-balance text-muted-foreground">
					{m.auth_forgotPassword_subtitle()}
				</p>
			</div>

			<form class="space-y-6" onsubmit={submit}>
				<div class="grid gap-2">
					<Label for="email">{m.auth_forgotPassword_email()}</Label>
					<Input id="email" type="email" autocomplete="email" required bind:value={email} />
				</div>

				<TurnstileWidget
					siteKey={turnstileSiteKey}
					onVerified={(t) => (captchaToken = t)}
					onError={() => toast.error(m.auth_captcha_error())}
					resetRef={(fn) => (resetCaptcha = fn)}
				/>

				<Button
					type="submit"
					class="w-full"
					disabled={isLoading || cooldown.active || !captchaToken}
				>
					{#if cooldown.active}
						{m.common_waitSeconds({ seconds: cooldown.remaining })}
					{:else if isLoading}
						{m.auth_forgotPassword_submitting()}
					{:else}
						{m.auth_forgotPassword_submit()}
					{/if}
				</Button>
			</form>

			<div class="text-center text-sm">
				<a
					href={resolve(routes.login)}
					class="inline-flex min-h-11 items-center font-medium text-primary hover:underline"
				>
					{m.common_backToLogin()}
				</a>
			</div>
		</div>
	{:else}
		<div class="flex flex-col items-center gap-4 py-4">
			<IconCircle icon={MailCheck} variant="success" />
			<div class="flex flex-col items-center gap-2 text-center">
				<h1 class="text-2xl font-bold tracking-tight">{m.auth_forgotPassword_successTitle()}</h1>
				<p class="text-sm text-balance text-muted-foreground">
					{m.auth_forgotPassword_successDescription()}
				</p>
			</div>
			<a
				href={resolve(routes.login)}
				class="inline-flex min-h-11 items-center text-sm font-medium text-primary hover:underline"
			>
				{m.common_backToLogin()}
			</a>
		</div>
	{/if}
</AuthShell>
