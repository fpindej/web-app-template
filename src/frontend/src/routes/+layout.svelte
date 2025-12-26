<script lang="ts">
	import './layout.css';
	import 'flag-icons/css/flag-icons.min.css';
	import favicon from '$lib/assets/favicon.svg';
	import { onMount, untrack } from 'svelte';
	import { initTheme } from '$lib/theme.svelte';
	import { initI18n } from '$lib/i18n';

	let { children, data } = $props();

	initI18n(untrack(() => data.locale));

	$effect(() => {
		initI18n(data.locale);
	});

	onMount(() => {
		return initTheme();
	});
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
	<title>MyProject</title>
</svelte:head>

{@render children()}
