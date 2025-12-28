<script lang="ts">
	import './layout.css';
	import 'flag-icons/css/flag-icons.min.css';
	import favicon from '$lib/assets/favicon.svg';
	import { onMount } from 'svelte';
	import { initTheme } from '$lib/theme.svelte';
	import * as m from '$lib/paraglide/messages';
	import { Toaster } from '$lib/components/ui/sonner';

	let { children } = $props();

	onMount(() => {
		return initTheme();
	});
</script>

<svelte:head>
	<title>{m.common_appName()}</title>
	<meta name="description" content={m.common_meta_description()} />
	<link rel="icon" href={favicon} />
</svelte:head>

<Toaster />
{@render children()}
