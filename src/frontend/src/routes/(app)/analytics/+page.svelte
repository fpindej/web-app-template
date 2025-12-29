<script lang="ts">
	import { WorkInProgress } from '$lib/components/common';
	import * as m from '$lib/paraglide/messages';
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_analytics_title() })}</title>
	<meta name="description" content={m.meta_analytics_description()} />
</svelte:head>

<WorkInProgress
	title={m.nav_analytics()}
	wipTitle={m.wip_templateTitle()}
	wipDescription={m.wip_templateDescription()}
/>
