<script lang="ts">
	import { Construction } from '@lucide/svelte';
	import * as Card from '$lib/components/ui/card';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		title: string;
		description?: string;
		wipTitle?: string;
		wipDescription?: string;
	}

	let { title, description, wipTitle, wipDescription }: Props = $props();
</script>

<div class="space-y-8">
	<div>
		<h3 class="text-lg font-medium">{title}</h3>
		{#if description}
			<p class="text-sm text-muted-foreground">{description}</p>
		{/if}
	</div>
	<Card.Root
		class="relative flex min-h-96 flex-col items-center justify-center overflow-hidden p-16 text-center"
	>
		<!-- Ambient glow -->
		<div class="glow-xl-top-end opacity-50"></div>
		<div class="glow-xl-bottom-start opacity-50"></div>

		<!-- Icon container with warning theme -->
		<div class="relative mb-6">
			<div class="glow-warning"></div>
			<div
				class="relative flex h-20 w-20 items-center justify-center rounded-full border border-warning/30 bg-gradient-to-br from-warning/10 to-amber-500/10"
			>
				<Construction class="h-10 w-10 text-warning" />
			</div>
		</div>

		<h2 class="relative text-2xl font-bold tracking-tight">{wipTitle ?? m.wip_title()}</h2>
		<p class="relative mt-4 max-w-lg text-muted-foreground">
			{wipDescription ?? m.wip_description()}
		</p>
	</Card.Root>
</div>
