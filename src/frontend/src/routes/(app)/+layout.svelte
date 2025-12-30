<script lang="ts">
	import { Header, Sidebar } from '$lib/components/layout';
	import { page } from '$app/state';
	import { onMount } from 'svelte';
	import { initSidebar, sidebarState } from '$lib/state';

	let { children, data } = $props();

	let collapsed = $derived(sidebarState.collapsed);

	onMount(() => {
		initSidebar();
	});
</script>

<div
	class="grid h-dvh w-full overflow-x-hidden transition-[grid-template-columns] duration-300 md:grid-cols-[var(--sidebar-width)_1fr]"
	style="--sidebar-width: {collapsed
		? 'var(--sidebar-width-collapsed)'
		: 'var(--sidebar-width-md)'};"
>
	<div class="hidden border-e bg-muted/40 md:block">
		<Sidebar class="h-full" user={data.user} />
	</div>
	<div class="flex flex-col overflow-hidden">
		<Header user={data.user} />
		<main class="flex flex-1 flex-col gap-4 overflow-y-auto p-4 lg:gap-6 lg:p-6">
			{#key page.url.pathname}
				<div
					class="motion-safe:duration-300 motion-safe:animate-in motion-safe:fade-in motion-safe:slide-in-from-bottom-4"
				>
					{@render children()}
				</div>
			{/key}
		</main>
	</div>
</div>
