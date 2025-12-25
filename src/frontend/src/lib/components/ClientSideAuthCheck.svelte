<script lang="ts">
	import { client } from '$lib/api/client';
	import type { components } from '$lib/api/v1';
	import { onMount } from 'svelte';
	import * as Card from '$lib/components/ui/card';
	import { Badge } from '$lib/components/ui/badge';

	let clientUser = $state<components['schemas']['MeResponse'] | null>(null);
	let loading = $state(true);
	let error = $state('');

	onMount(async () => {
		try {
			const { data, response } = await client.GET('/api/Auth/me');
			if (response.ok && data) {
				clientUser = data;
			} else {
				error = 'Failed to fetch user client-side';
			}
		} catch {
			error = 'Error fetching user client-side';
		} finally {
			loading = false;
		}
	});
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>Client-Side Auth Check</Card.Title>
		<Card.Description>Verifying authentication from the browser.</Card.Description>
	</Card.Header>
	<Card.Content>
		<dl class="sm:divide-y sm:divide-border">
			<div class="py-4 sm:grid sm:grid-cols-3 sm:gap-4 sm:py-5">
				<dt class="text-sm font-medium text-muted-foreground">Status</dt>
				<dd class="mt-1 text-sm text-foreground sm:col-span-2 sm:mt-0">
					{#if loading}
						<Badge variant="warning">Loading...</Badge>
					{:else if error}
						<Badge variant="destructive">Error</Badge>
					{:else}
						<Badge variant="success">Authenticated</Badge>
					{/if}
				</dd>
			</div>
			{#if clientUser}
				<div class="py-4 sm:grid sm:grid-cols-3 sm:gap-4 sm:py-5">
					<dt class="text-sm font-medium text-muted-foreground">Username (Client Fetch)</dt>
					<dd class="mt-1 text-sm text-foreground sm:col-span-2 sm:mt-0">{clientUser.username}</dd>
				</div>
				<div class="py-4 sm:grid sm:grid-cols-3 sm:gap-4 sm:py-5">
					<dt class="text-sm font-medium text-muted-foreground">Roles</dt>
					<dd class="mt-1 text-sm text-foreground sm:col-span-2 sm:mt-0">
						{clientUser.roles?.join(', ') || 'None'}
					</dd>
				</div>
			{/if}
			{#if error}
				<div class="py-4 sm:grid sm:grid-cols-3 sm:gap-4 sm:py-5">
					<dt class="text-sm font-medium text-muted-foreground">Error Details</dt>
					<dd class="mt-1 text-sm text-destructive sm:col-span-2 sm:mt-0">{error}</dd>
				</div>
			{/if}
		</dl>
	</Card.Content>
</Card.Root>
