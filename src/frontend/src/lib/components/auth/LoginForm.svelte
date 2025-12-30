<script lang="ts">
	import { browserClient } from '$lib/api';
	import { cn } from '$lib/utils';
	import { createShake } from '$lib/state';
	import { onMount } from 'svelte';
	import { goto, invalidateAll } from '$app/navigation';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import * as Card from '$lib/components/ui/card';
	import { ThemeToggle, LanguageSelector } from '$lib/components/layout';
	import { StatusIndicator } from '$lib/components/common';
	import * as m from '$lib/paraglide/messages';
	import { fly, scale } from 'svelte/transition';
	import { Check } from '@lucide/svelte';
	import { LoginBackground, RegisterDialog } from '$lib/components/auth';
	import { toast } from '$lib/components/ui/sonner';

	let { apiUrl } = $props();

	let email = $state('');
	let password = $state('');
	let isApiOnline = $state(false);
	let isSuccess = $state(false);
	let isRedirecting = $state(false);
	const shake = createShake();
	let isRegisterOpen = $state(false);

	const delay = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

	onMount(async () => {
		try {
			const res = await fetch('/api/health');
			isApiOnline = res.ok;
		} catch {
			isApiOnline = false;
		}
	});

	function onRegisterSuccess(newEmail: string) {
		email = newEmail;
		password = '';
	}

	async function login(e: Event) {
		e.preventDefault();

		try {
			const { response, error: apiError } = await browserClient.POST('/api/auth/login', {
				body: { username: email, password }
			});

			if (response.ok) {
				isSuccess = true;
				await delay(1500);
				isRedirecting = true;
				await delay(500);
				await invalidateAll();
				// eslint-disable-next-line svelte/no-navigation-without-resolve
				await goto('/');
			} else {
				let errorMessage = '';
				if (response.status === 401) {
					errorMessage = m.auth_login_invalidCredentials();
				} else {
					errorMessage = apiError?.detail || apiError?.title || m.auth_login_error();
				}
				toast.error(m.auth_login_failed(), {
					description: errorMessage
				});
				shake.trigger();
			}
		} catch {
			toast.error(m.auth_login_failed(), {
				description: m.auth_login_error()
			});
			shake.trigger();
		}
	}
</script>

<LoginBackground>
	<div class="absolute end-4 top-4 flex gap-2">
		<LanguageSelector />
		<ThemeToggle />
	</div>

	<!-- Subtle API status indicator for debugging -->
	<div
		class="group absolute start-4 bottom-4 flex cursor-default items-center gap-2 rounded-lg px-2 py-1 text-xs text-muted-foreground/60 transition-all hover:bg-muted/50 hover:text-muted-foreground"
		title={apiUrl}
	>
		<StatusIndicator status={isApiOnline ? 'online' : 'offline'} size="sm" />
		<span class="hidden group-hover:inline">{apiUrl}</span>
	</div>

	{#if !isSuccess}
		<div
			class="sm:mx-auto sm:w-full sm:max-w-md"
			in:fly={{ y: 20, duration: 600, delay: 100 }}
			out:scale={{ duration: 400, start: 1, opacity: 0 }}
		>
			<Card.Root
				class={cn(
					'border-muted/60 bg-card/50 shadow-xl backdrop-blur-sm transition-colors duration-300',
					shake.active && 'animate-shake border-destructive'
				)}
			>
				<Card.Header>
					<Card.Title class="text-center text-2xl">{m.auth_login_title()}</Card.Title>
					<Card.Description class="text-center">{m.auth_login_subtitle()}</Card.Description>
				</Card.Header>
				<Card.Content>
					<form class="space-y-6" onsubmit={login}>
						<div class="grid gap-2">
							<Label for="email">{m.auth_login_email()}</Label>
							<Input
								id="email"
								type="email"
								autocomplete="email"
								required
								bind:value={email}
								class="bg-background/50"
								aria-invalid={shake.active}
							/>
						</div>

						<div class="grid gap-2">
							<Label for="password">{m.auth_login_password()}</Label>
							<Input
								id="password"
								type="password"
								autocomplete="current-password"
								required
								bind:value={password}
								class="bg-background/50"
								aria-invalid={shake.active}
							/>
						</div>

						<Button type="submit" class="w-full" disabled={!isApiOnline}>
							{isApiOnline ? m.auth_login_submit() : m.auth_login_apiOffline()}
						</Button>
					</form>
					<div class="mt-4 text-center text-sm">
						<span class="text-muted-foreground">{m.auth_login_noAccount()}</span>
						<button
							type="button"
							class="ms-1 font-medium text-primary hover:underline"
							onclick={() => (isRegisterOpen = true)}
						>
							{m.auth_login_signUp()}
						</button>
					</div>
				</Card.Content>
			</Card.Root>
		</div>
	{:else if !isRedirecting}
		<div
			class="flex flex-col items-center justify-center gap-4"
			in:scale={{ duration: 500, delay: 400, start: 0.8, opacity: 0 }}
			out:scale={{ duration: 500, start: 1.2, opacity: 0 }}
		>
			<div
				class="flex h-24 w-24 items-center justify-center rounded-full bg-success text-success-foreground shadow-2xl"
			>
				<Check class="h-12 w-12" />
			</div>
			<h2 class="text-3xl font-bold tracking-tight text-foreground">
				{m.auth_login_success()}
			</h2>
		</div>
	{/if}
</LoginBackground>

<RegisterDialog bind:open={isRegisterOpen} onSuccess={onRegisterSuccess} />
