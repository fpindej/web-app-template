<script lang="ts">
	import { Button } from '$lib/components/ui/button';
	import * as Sheet from '$lib/components/ui/sheet';
	import { Menu } from 'lucide-svelte';
	import SidebarNav from './SidebarNav.svelte';
	import ThemeToggle from './ThemeToggle.svelte';
	import LanguageSelector from './LanguageSelector.svelte';
	import UserNav from './UserNav.svelte';
	import { base } from '$app/paths';
	import { Package2 } from 'lucide-svelte';
	import type { components } from '$lib/api/v1';

	type User = components['schemas']['MeResponse'];

	let { user }: { user: User | null | undefined } = $props();
	let open = $state(false);
</script>

<!-- eslint-disable svelte/no-navigation-without-resolve -->
<header class="flex h-14 items-center gap-4 border-b bg-muted/40 px-4 md:hidden">
	<Sheet.Root bind:open>
		<Sheet.Trigger>
			{#snippet child({ props })}
				<Button variant="outline" size="icon" class="shrink-0 md:hidden" {...props}>
					<Menu class="h-5 w-5" />
					<span class="sr-only">Toggle navigation menu</span>
				</Button>
			{/snippet}
		</Sheet.Trigger>
		<Sheet.Content side="left" class="flex flex-col">
			<nav class="grid gap-2 text-lg font-medium">
				<a href="{base}/" class="flex items-center gap-2 text-lg font-semibold">
					<Package2 class="h-6 w-6" />
					<span class="sr-only">MyProject</span>
				</a>
				<SidebarNav />
			</nav>
		</Sheet.Content>
	</Sheet.Root>

	<div class="w-full flex-1">
		<!-- Search or other items could go here -->
	</div>
	<nav class="flex items-center gap-2">
		<LanguageSelector />
		<ThemeToggle />
		{#if user}
			<UserNav {user} />
		{/if}
	</nav>
</header>
