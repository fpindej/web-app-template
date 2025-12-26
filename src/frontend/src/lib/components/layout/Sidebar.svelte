<script lang="ts">
	import { cn } from '$lib/utils';
	import SidebarNav from './SidebarNav.svelte';
	import { Package2 } from 'lucide-svelte';
	import { base } from '$app/paths';
	import ThemeToggle from './ThemeToggle.svelte';
	import LanguageSelector from './LanguageSelector.svelte';
	import UserNav from './UserNav.svelte';
	import type { components } from '$lib/api/v1';
	import { t } from '$lib/i18n';

	type User = components['schemas']['MeResponse'];

	let { class: className, user }: { class?: string; user: User | null | undefined } = $props();
</script>

<div class={cn('flex h-full flex-col gap-2', className)}>
	<div class="flex-1 overflow-auto py-4">
		<div class="px-3 py-2">
			<div class="mb-2 px-4">
				<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
				<a href="{base}/" class="flex items-center gap-2 text-lg font-semibold">
					<Package2 class="h-6 w-6" />
					<span class="">{$t('common.appName')}</span>
				</a>
			</div>
			<div class="space-y-1">
				<SidebarNav />
			</div>
		</div>
	</div>
	<div class="border-t p-4">
		<div class="flex items-center justify-between gap-2">
			<div class="flex items-center gap-2">
				<LanguageSelector />
				<ThemeToggle />
			</div>
			{#if user}
				<UserNav {user} />
			{/if}
		</div>
	</div>
</div>
