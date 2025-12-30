<script lang="ts">
	import { Button } from '$lib/components/ui/button';
	import * as Sheet from '$lib/components/ui/sheet';
	import { Menu, Package2 } from '@lucide/svelte';
	import { SidebarNav, ThemeToggle, LanguageSelector, UserNav } from '$lib/components/layout';
	import { resolve } from '$app/paths';
	import type { User } from '$lib/types';

	interface Props {
		user: User | null | undefined;
	}

	let { user }: Props = $props();
	let open = $state(false);
</script>

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
				<a href={resolve('/')} class="flex items-center gap-2 text-lg font-semibold">
					<Package2 class="h-6 w-6" />
					<span class="sr-only">MyProject</span>
				</a>
				<SidebarNav onNavigate={() => (open = false)} />
			</nav>
		</Sheet.Content>
	</Sheet.Root>

	<!-- Flex spacer to push the right-side navigation items to the edge on mobile -->
	<div class="w-full flex-1"></div>
	<nav class="flex items-center gap-2">
		<LanguageSelector />
		<ThemeToggle />
		{#if user}
			<UserNav {user} />
		{/if}
	</nav>
</header>
