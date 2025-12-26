<script lang="ts">
	import './layout.css';
	import 'flag-icons/css/flag-icons.min.css';
	import favicon from '$lib/assets/favicon.svg';
	import { onMount } from 'svelte';
	import { initTheme } from '$lib/theme.svelte';
	import { t } from '$lib/i18n';

	let { children } = $props();

	onMount(() => {
		return initTheme();
	});
</script>

<svelte:head>
	<title>{$t('common.appName')}</title>
	<meta name="description" content={$t('common.meta.description')} />
	<link rel="icon" href={favicon} />
</svelte:head>

{@render children()}
