<script lang="ts">
	import '../styles/index.css';
	import 'flag-icons/css/flag-icons.min.css';
	import favicon from '$lib/assets/favicon.svg';
	import { onMount } from 'svelte';
	import { initTheme } from '$lib/state/theme.svelte';
	import * as m from '$lib/paraglide/messages';
	import { Toaster } from '$lib/components/ui/sonner';
	import * as Tooltip from '$lib/components/ui/tooltip';
	import { globalShortcuts } from '$lib/state/shortcuts.svelte';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { logout } from '$lib/auth';
	import { ShortcutsHelp } from '$lib/components/layout';
	import { toggleSidebar } from '$lib/state';

	let { children } = $props();

	onMount(() => {
		return initTheme();
	});

	async function handleSettings() {
		await goto(resolve('/settings'));
	}
</script>

<svelte:window
	use:globalShortcuts={{
		settings: handleSettings,
		logout: logout,
		toggleSidebar: toggleSidebar
	}}
/>

<ShortcutsHelp />

<svelte:head>
	<title>{m.app_name()}</title>
	<meta name="description" content={m.meta_description()} />
	<link rel="icon" href={favicon} />
</svelte:head>

<Tooltip.Provider>
	<Toaster />
	{@render children()}
</Tooltip.Provider>
