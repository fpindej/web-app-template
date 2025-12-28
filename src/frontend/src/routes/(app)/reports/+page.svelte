<script lang="ts">
	import { WorkInProgress } from '$lib/components/common';
	import * as m from '$lib/paraglide/messages';
</script>

<svelte:head>
	<title>{m.common_meta_titleTemplate({ title: m.common_meta_reports_title() })}</title>
	<meta name="description" content={m.common_meta_reports_description()} />
</svelte:head>

<WorkInProgress
	title={m.common_reports()}
	wipTitle={m.common_template_example_title()}
	wipDescription={m.common_template_example_description()}
/>
