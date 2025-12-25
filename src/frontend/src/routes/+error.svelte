<script lang="ts">
	import { page } from '$app/stores';
	import { base } from '$app/paths';
	import { Button } from '$lib/components/ui/button';
	import * as Card from '$lib/components/ui/card';
	import { Ghost, Ban, TriangleAlert, Home, SearchX } from 'lucide-svelte';
	import { t } from '$lib/i18n';

	function getErrorContent(status: number) {
		switch (status) {
			case 404:
				return {
					title: 'error.404.title',
					description: 'error.404.description',
					icon: SearchX,
					iconColor: 'text-muted-foreground'
				};
			case 403:
				return {
					title: 'error.403.title',
					description: 'error.403.description',
					icon: Ban,
					iconColor: 'text-destructive'
				};
			case 500:
				return {
					title: 'error.500.title',
					description: 'error.500.description',
					icon: TriangleAlert,
					iconColor: 'text-destructive'
				};
			default:
				return {
					title: 'error.default.title',
					description: 'error.default.description',
					icon: Ghost,
					iconColor: 'text-warning'
				};
		}
	}

	let status = $derived($page.status);
	let message = $derived($page.error?.message);
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
					{$t(content.title)}
				</Card.Description>
			</Card.Header>
			<Card.Content>
				<p class="text-muted-foreground">
					{message && message !== 'An unexpected error occurred.'
						? message
						: $t(content.description)}
				</p>
			</Card.Content>
			<Card.Footer class="flex justify-center pb-8">
				<Button href="{base}/" variant="default" size="lg" class="gap-2">
					<Home class="h-4 w-4" />
					{$t('error.goHome')}
				</Button>
			</Card.Footer>
		</Card.Root>
	</div>
</div>
