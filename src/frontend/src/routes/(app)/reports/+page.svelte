<script lang="ts">
	import { WorkInProgress } from '$lib/components/common';
	import * as m from '$lib/paraglide/messages';
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_reports_title() })}</title>
	<meta name="description" content={m.meta_reports_description()} />
</svelte:head>

<WorkInProgress
	title={m.nav_reports()}
	wipTitle={m.wip_templateTitle()}
	wipDescription={m.wip_templateDescription()}
/>
