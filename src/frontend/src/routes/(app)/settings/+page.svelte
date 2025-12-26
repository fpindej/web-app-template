<script lang="ts">
	import WorkInProgress from '$lib/components/WorkInProgress.svelte';
	import { t } from '$lib/i18n';
</script>

<WorkInProgress
	title={$t('settings.title')}
	description={$t('settings.description')}
	wipDescription={$t('settings.wipDescription')}
/>
