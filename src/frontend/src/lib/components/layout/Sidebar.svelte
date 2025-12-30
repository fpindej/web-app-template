<script lang="ts">
	import { cn } from '$lib/utils';
	import { SidebarNav, ThemeToggle, LanguageSelector, UserNav } from '$lib/components/layout';
	import { Package2 } from '@lucide/svelte';
	import { resolve } from '$app/paths';
	import type { User } from '$lib/types';
	import * as m from '$lib/paraglide/messages';

	let { class: className, user }: { class?: string; user: User | null | undefined } = $props();
</script>

<div class={cn('flex h-full flex-col gap-2', className)}>
	<div class="flex-1 overflow-auto py-4">
		<div class="px-3 py-2">
			<div class="mb-2 px-4">
				<a href={resolve('/')} class="flex items-center gap-2 text-lg font-semibold">
					<Package2 class="h-6 w-6" />
					<span class="">{m.app_name()}</span>
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
