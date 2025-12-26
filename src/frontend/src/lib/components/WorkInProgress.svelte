<script lang="ts">
	import { Construction } from 'lucide-svelte';
	import * as Card from '$lib/components/ui/card';
	import { t } from '$lib/i18n';

	let {
		title,
		description,
		wipTitle,
		wipDescription
	}: {
		title: string;
		description?: string;
		wipTitle?: string;
		wipDescription?: string;
	} = $props();
</script>

<div class="space-y-8">
	<div>
		<h3 class="text-lg font-medium">{title}</h3>
		{#if description}
			<p class="text-sm text-muted-foreground">{description}</p>
		{/if}
	</div>
	<Card.Root
		class="flex min-h-96 flex-col items-center justify-center p-16 text-center duration-500 animate-in fade-in zoom-in-95"
	>
		<div
			class="mb-6 flex h-20 w-20 animate-bounce items-center justify-center rounded-full bg-muted"
		>
			<Construction class="h-10 w-10 text-muted-foreground" />
		</div>
		<h2 class="text-2xl font-bold tracking-tight">{wipTitle ?? $t('common.wip.title')}</h2>
		<p class="mt-4 max-w-lg text-muted-foreground">
			{wipDescription ?? $t('common.wip.description')}
		</p>
	</Card.Root>
</div>
