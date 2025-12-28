<script lang="ts">
	import { WorkInProgress } from '$lib/components/common';
	import * as m from '$lib/paraglide/messages';
</script>

<svelte:head>
	<title>{m.common_meta_titleTemplate({ title: m.common_meta_settings_title() })}</title>
	<meta name="description" content={m.common_meta_settings_description()} />
</svelte:head>

<WorkInProgress
	title={m.settings_title()}
	description={m.settings_description()}
	wipDescription={m.settings_wipDescription()}
/>
