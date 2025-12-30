<script lang="ts">
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import { Button } from '$lib/components/ui/button';
	import * as Card from '$lib/components/ui/card';
	import { Ghost, Ban, TriangleAlert, Home, SearchX, type IconProps } from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';
	import type { Component } from 'svelte';

	function getErrorContent(status: number): {
		title: () => string;
		description: () => string;
		icon: Component<IconProps>;
		iconColor: string;
	} {
		switch (status) {
			case 404:
				return {
					title: m.error_404_title,
					description: m.error_404_description,
					icon: SearchX,
					iconColor: 'text-muted-foreground'
				};
			case 403:
				return {
					title: m.error_403_title,
					description: m.error_403_description,
					icon: Ban,
					iconColor: 'text-destructive'
				};
			case 500:
				return {
					title: m.error_500_title,
					description: m.error_500_description,
					icon: TriangleAlert,
					iconColor: 'text-destructive'
				};
			default:
				return {
					title: m.error_default_title,
					description: m.error_default_description,
					icon: Ghost,
					iconColor: 'text-warning'
				};
		}
	}

	let status = $derived(page.status);
	let message = $derived(page.error?.message);
	let content = $derived(getErrorContent(status));
	let Icon = $derived(content.icon);
</script>

<div class="flex min-h-screen flex-col justify-center bg-background px-4 py-12 sm:px-6 lg:px-8">
	<div class="sm:mx-auto sm:w-full sm:max-w-md">
		<Card.Root class="text-center shadow-lg">
			<Card.Header>
				<div
					class="mx-auto mb-4 flex h-24 w-24 items-center justify-center rounded-full bg-muted/50 p-4"
				>
					<Icon class="h-12 w-12 {content.iconColor}" />
				</div>
				<Card.Title class="text-4xl font-extrabold tracking-tight">{status}</Card.Title>
				<Card.Description class="mt-2 text-xl font-semibold text-foreground">
					{content.title()}
				</Card.Description>
			</Card.Header>
			<Card.Content>
				<p class="text-muted-foreground">
					{message && message !== 'An unexpected error occurred.' ? message : content.description()}
				</p>
			</Card.Content>
			<Card.Footer class="flex justify-center pb-8">
				<Button href={resolve('/')} variant="default" size="lg" class="gap-2">
					<Home class="h-4 w-4" />
					{m.error_goHome()}
				</Button>
			</Card.Footer>
		</Card.Root>
	</div>
</div>
