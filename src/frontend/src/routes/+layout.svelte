<script lang="ts">
	import './layout.css';
	import 'flag-icons/css/flag-icons.min.css';
	import favicon from '$lib/assets/favicon.svg';
	import { onMount } from 'svelte';
	import { initTheme } from '$lib/theme.svelte';
	import { initI18n } from '$lib/i18n';
	import { isLoading } from 'svelte-i18n';

	let { children } = $props();

	initI18n();

	onMount(() => {
		initTheme();
	});
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
	<title>MyProject</title>
</svelte:head>

{#if $isLoading}
	<div class="flex h-screen w-full items-center justify-center">
		<div
			class="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent"
		></div>
	</div>
{:else}
	{@render children()}
{/if}
