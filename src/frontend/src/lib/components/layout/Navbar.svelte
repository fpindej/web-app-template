<script lang="ts">
	import { client } from '$lib/api/client';
	import { base } from '$app/paths';
	import { goto, invalidateAll } from '$app/navigation';
	import { Button } from '$lib/components/ui/button';
	import ThemeToggle from '$lib/components/ThemeToggle.svelte';
	import LanguageSelector from '$lib/components/LanguageSelector.svelte';
	import { t } from '$lib/i18n';

	let { user } = $props();
</script>

<!-- eslint-disable svelte/no-navigation-without-resolve -->
<nav class="border-b border-border bg-card">
	<div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
		<div class="flex h-16 justify-between">
			<div class="flex">
				<div class="flex flex-shrink-0 items-center">
					<a href="{base}/" class="text-xl font-bold text-primary">MyProject</a>
				</div>
			</div>
			<div class="flex items-center gap-4">
				<LanguageSelector />
				<ThemeToggle />
				{#if user}
					<span class="mr-4 hidden text-sm text-muted-foreground sm:inline-block"
						>{$t('common.hello')}, {user.username || 'User'}</span
					>
					<div class="w-24">
						<Button
							onclick={async () => {
								await client.POST('/api/auth/logout');
								await invalidateAll();
								await goto('/login');
							}}
						>
							{$t('navbar.logout')}
						</Button>
					</div>
				{/if}
			</div>
		</div>
	</div>
</nav>
