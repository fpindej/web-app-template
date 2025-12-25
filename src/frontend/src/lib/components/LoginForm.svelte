<script lang="ts">
	import { client } from '$lib/api/client';
	import { cn } from '$lib/utils';
	import { onMount } from 'svelte';
	import { goto, invalidateAll } from '$app/navigation';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import * as Card from '$lib/components/ui/card';
	import * as Alert from '$lib/components/ui/alert';
	import { CircleAlert } from 'lucide-svelte';
	import ThemeToggle from '$lib/components/ThemeToggle.svelte';
	import LanguageSelector from '$lib/components/LanguageSelector.svelte';
	import { t } from '$lib/i18n';

	let { apiUrl } = $props();

	let email = $state('');
	let password = $state('');
	let error = $state('');
	let isApiOnline = $state(false);

	onMount(async () => {
		try {
			const res = await fetch('/api/health');
			isApiOnline = res.ok;
		} catch {
			isApiOnline = false;
		}
	});

	async function login(e: Event) {
		e.preventDefault();
		error = '';

		try {
			const { response, error: apiError } = await client.POST('/api/auth/login', {
				body: { username: email, password }
			});

			if (response.ok) {
				await invalidateAll();
				// eslint-disable-next-line svelte/no-navigation-without-resolve
				await goto('/');
			} else {
				error = apiError?.detail || apiError?.title || 'common.login.failed';
			}
		} catch {
			error = 'common.login.error';
		}
	}
</script>

<div class="flex min-h-screen flex-col justify-center px-4 py-8 sm:px-6 sm:py-12 lg:px-8">
	<div class="absolute top-4 right-4 flex gap-2">
		<LanguageSelector />
		<ThemeToggle />
	</div>
	<div class="sm:mx-auto sm:w-full sm:max-w-md">
		<Card.Root>
			<Card.Header>
				<Card.Title class="text-center text-2xl">{$t('common.login.title')}</Card.Title>
				<Card.Description class="text-center">{$t('common.login.subtitle')}</Card.Description>

				<div class="mt-4 flex justify-center">
					<div
						class="group flex items-center gap-x-2 rounded-full bg-secondary px-4 py-1 text-sm font-medium text-secondary-foreground shadow-sm ring-1 ring-border hover:bg-secondary/80"
					>
						<div
							class={cn('h-1.5 w-1.5 rounded-full', isApiOnline ? 'bg-success' : 'bg-destructive')}
						></div>
						<span class="group-hover:hidden"
							>{isApiOnline ? $t('common.login.apiOnline') : $t('common.login.apiOffline')}</span
						>
						<span class="hidden group-hover:block">{apiUrl}</span>
					</div>
				</div>
			</Card.Header>
			<Card.Content>
				<form class="space-y-6" onsubmit={login}>
					<div class="grid gap-2">
						<Label for="email">{$t('common.login.email')}</Label>
						<Input id="email" type="email" autocomplete="email" required bind:value={email} />
					</div>

					<div class="grid gap-2">
						<Label for="password">{$t('common.login.password')}</Label>
						<Input
							id="password"
							type="password"
							autocomplete="current-password"
							required
							bind:value={password}
						/>
					</div>

					{#if error}
						<Alert.Root variant="destructive">
							<CircleAlert class="h-4 w-4" />
							<Alert.Title>{$t('dashboard.clientSideAuth.error')}</Alert.Title>
							<Alert.Description>{$t(error)}</Alert.Description>
						</Alert.Root>
					{/if}

					<Button type="submit" class="w-full" disabled={!isApiOnline}>
						{isApiOnline ? $t('common.login.submit') : $t('common.login.apiOffline')}
					</Button>
				</form>
			</Card.Content>
		</Card.Root>
	</div>
</div>
