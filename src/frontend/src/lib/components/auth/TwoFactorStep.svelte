<script lang="ts">
	import { browserClient, getErrorMessage, handleMutationError } from '$lib/api';
	import { cn } from '$lib/utils';
	import { createShake, createCooldown } from '$lib/state';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import * as InputOTP from '$lib/components/ui/input-otp';
	import * as m from '$lib/paraglide/messages';
	import { Loader2, ArrowLeft } from '@lucide/svelte';
	import { toast } from '$lib/components/ui/sonner';

	interface Props {
		challengeToken: string;
		onSuccess: () => Promise<void>;
		onBack: () => void;
	}

	let { challengeToken, onSuccess, onBack }: Props = $props();

	let code = $state('');
	let recoveryCode = $state('');
	let isLoading = $state(false);
	let useRecovery = $state(false);
	const shake = createShake();
	const cooldown = createCooldown();

	function handleOtpComplete(value: string) {
		code = value;
		if (value.length === 6 && !isLoading && !cooldown.active) {
			submitCode();
		}
	}

	async function submitCode(e?: Event) {
		e?.preventDefault();
		if (isLoading || cooldown.active || code.length !== 6) return;
		isLoading = true;

		try {
			const { response, error: apiError } = await browserClient.POST('/api/auth/two-factor/login', {
				body: { challengeToken, code }
			});

			if (response.ok) {
				await onSuccess();
			} else {
				handleMutationError(response, apiError, {
					cooldown,
					fallback: m.auth_twoFactor_invalidCode(),
					onRateLimited: () => shake.trigger(),
					onError() {
						toast.error(m.auth_login_failed(), {
							description: getErrorMessage(apiError, m.auth_twoFactor_invalidCode())
						});
						shake.trigger();
						code = '';
					}
				});
			}
		} catch {
			toast.error(m.auth_login_failed(), {
				description: m.auth_login_error()
			});
			shake.trigger();
			code = '';
		} finally {
			isLoading = false;
		}
	}

	async function submitRecoveryCode(e: Event) {
		e.preventDefault();
		if (isLoading || cooldown.active) return;
		isLoading = true;

		try {
			const { response, error: apiError } = await browserClient.POST(
				'/api/auth/two-factor/login/recovery',
				{
					body: { challengeToken, recoveryCode }
				}
			);

			if (response.ok) {
				await onSuccess();
			} else {
				handleMutationError(response, apiError, {
					cooldown,
					fallback: m.auth_twoFactor_invalidCode(),
					onRateLimited: () => shake.trigger(),
					onError() {
						toast.error(m.auth_login_failed(), {
							description: getErrorMessage(apiError, m.auth_twoFactor_invalidCode())
						});
						shake.trigger();
					}
				});
			}
		} catch {
			toast.error(m.auth_login_failed(), {
				description: m.auth_login_error()
			});
			shake.trigger();
		} finally {
			isLoading = false;
		}
	}
</script>

<div class={cn('flex flex-col gap-6', shake.active && 'animate-shake')}>
	<div class="flex flex-col items-center gap-2 text-center">
		<h1 class="text-2xl font-bold tracking-tight">{m.auth_twoFactor_title()}</h1>
		<p class="text-sm text-balance text-muted-foreground">
			{useRecovery ? m.auth_twoFactor_recoveryDescription() : m.auth_twoFactor_description()}
		</p>
	</div>

	{#if !useRecovery}
		<form class="space-y-6" onsubmit={submitCode}>
			<div class="flex flex-col items-center gap-2">
				<Label>{m.auth_twoFactor_codeLabel()}</Label>
				<InputOTP.Root
					maxlength={6}
					inputmode="numeric"
					autocomplete="one-time-code"
					bind:value={code}
					onComplete={handleOtpComplete}
					disabled={isLoading}
				>
					{#snippet children({ cells })}
						<InputOTP.Group>
							{#each cells.slice(0, 3) as cell (cell)}
								<InputOTP.Slot {cell} />
							{/each}
						</InputOTP.Group>
						<InputOTP.Separator />
						<InputOTP.Group>
							{#each cells.slice(3, 6) as cell (cell)}
								<InputOTP.Slot {cell} />
							{/each}
						</InputOTP.Group>
					{/snippet}
				</InputOTP.Root>
			</div>

			<Button
				type="submit"
				class="w-full"
				disabled={isLoading || cooldown.active || code.length !== 6}
			>
				{#if cooldown.active}
					{m.common_waitSeconds({ seconds: cooldown.remaining })}
				{:else}
					{#if isLoading}
						<Loader2 class="me-2 h-4 w-4 animate-spin" />
					{/if}
					{m.auth_twoFactor_submit()}
				{/if}
			</Button>
		</form>

		<div class="text-center">
			<Button
				variant="link"
				class="min-h-11 text-muted-foreground hover:text-primary focus-visible:text-primary focus-visible:underline"
				onclick={() => {
					useRecovery = true;
					code = '';
				}}
			>
				{m.auth_twoFactor_useRecoveryCode()}
			</Button>
		</div>
	{:else}
		<form class="space-y-6" onsubmit={submitRecoveryCode}>
			<div class="grid gap-2">
				<Label for="recoveryCode">{m.auth_twoFactor_recoveryCodeLabel()}</Label>
				<Input
					id="recoveryCode"
					type="text"
					autocomplete="off"
					placeholder={m.auth_twoFactor_recoveryCodePlaceholder()}
					required
					bind:value={recoveryCode}
					class="text-center tracking-wide"
					aria-invalid={shake.active}
					disabled={isLoading}
				/>
			</div>

			<Button
				type="submit"
				class="w-full"
				disabled={isLoading || cooldown.active || !recoveryCode.trim()}
			>
				{#if cooldown.active}
					{m.common_waitSeconds({ seconds: cooldown.remaining })}
				{:else}
					{#if isLoading}
						<Loader2 class="me-2 h-4 w-4 animate-spin" />
					{/if}
					{m.auth_twoFactor_submit()}
				{/if}
			</Button>
		</form>

		<div class="text-center">
			<Button
				variant="link"
				class="min-h-11 text-muted-foreground hover:text-primary focus-visible:text-primary focus-visible:underline"
				onclick={() => {
					useRecovery = false;
					recoveryCode = '';
				}}
			>
				{m.auth_twoFactor_backToCode()}
			</Button>
		</div>
	{/if}

	<div class="text-center">
		<Button
			variant="link"
			class="min-h-11 gap-1 text-muted-foreground hover:text-primary focus-visible:text-primary focus-visible:underline"
			onclick={onBack}
		>
			<ArrowLeft class="h-3 w-3" />
			{m.common_backToLogin()}
		</Button>
	</div>
</div>
