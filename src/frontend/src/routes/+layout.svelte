<script lang="ts">
	import './layout.css';
	import 'flag-icons/css/flag-icons.min.css';
	import favicon from '$lib/assets/favicon.svg';
	import { onMount } from 'svelte';
	import { initTheme } from '$lib/state/theme.svelte';
	import * as m from '$lib/paraglide/messages';
	import { Toaster } from '$lib/components/ui/sonner';
	import { globalShortcuts } from '$lib/state/shortcuts.svelte';
	import { goto } from '$app/navigation';
	import { base } from '$app/paths';
	import { logout } from '$lib/services/auth';
	import { ShortcutsHelp } from '$lib/components/layout';

	let { children } = $props();

	onMount(() => {
		return initTheme();
	});

	async function handleSettings() {
		// eslint-disable-next-line svelte/no-navigation-without-resolve
		await goto(`${base}/settings`);
	}
</script>

<svelte:window
	use:globalShortcuts={{
		settings: handleSettings,
		logout: logout
	}}
/>

<ShortcutsHelp />

<svelte:head>
	<title>{m.app_name()}</title>
	<meta name="description" content={m.meta_description()} />
	<link rel="icon" href={favicon} />
</svelte:head>

<Toaster />
{@render children()}
