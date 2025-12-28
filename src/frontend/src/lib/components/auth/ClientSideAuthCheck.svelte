<script lang="ts">
	import { browserClient } from '$lib/api/client';
	import type { components } from '$lib/api/v1';
	import { onMount } from 'svelte';
	import * as Card from '$lib/components/ui/card';
	import { Badge } from '$lib/components/ui/badge';
	import * as m from '$lib/paraglide/messages';

	let clientUser = $state<components['schemas']['MeResponse'] | null>(null);
	let loading = $state(true);
	let error = $state<(() => string) | null>(null);

	onMount(async () => {
		try {
			const { data, response } = await browserClient.GET('/api/auth/me');
			if (response.ok && data) {
				clientUser = data;
			} else {
				error = m.dashboard_clientSideAuth_failedFetch;
			}
		} catch {
			error = m.dashboard_clientSideAuth_errorFetch;
		} finally {
			loading = false;
		}
	});
</script>

<Card.Root>
	<Card.Header>
		<Card.Title class="text-lg">{m.dashboard_clientSideAuth_title()}</Card.Title>
		<Card.Description>{m.dashboard_clientSideAuth_description()}</Card.Description>
	</Card.Header>
	<Card.Content>
		<dl class="sm:divide-y sm:divide-border">
			<div class="py-4 sm:grid sm:grid-cols-3 sm:gap-4 sm:py-5">
				<dt class="text-sm font-medium text-muted-foreground">{m.dashboard_status()}</dt>
				<dd class="mt-1 text-sm text-foreground sm:col-span-2 sm:mt-0">
					{#if loading}
						<Badge variant="warning">{m.dashboard_clientSideAuth_loading()}</Badge>
					{:else if error}
						<Badge variant="destructive">{m.dashboard_clientSideAuth_error()}</Badge>
					{:else}
						<Badge variant="success">{m.dashboard_clientSideAuth_authenticated()}</Badge>
					{/if}
				</dd>
			</div>
			{#if clientUser}
				<div class="py-4 sm:grid sm:grid-cols-3 sm:gap-4 sm:py-5">
					<dt class="text-sm font-medium text-muted-foreground">
						{m.dashboard_clientSideAuth_usernameClient()}
					</dt>
					<dd class="mt-1 text-sm text-foreground sm:col-span-2 sm:mt-0">{clientUser.username}</dd>
				</div>
				<div class="py-4 sm:grid sm:grid-cols-3 sm:gap-4 sm:py-5">
					<dt class="text-sm font-medium text-muted-foreground">{m.dashboard_roles()}</dt>
					<dd class="mt-1 text-sm text-foreground sm:col-span-2 sm:mt-0">
						{clientUser.roles?.join(', ') || m.dashboard_none()}
					</dd>
				</div>
			{/if}
			{#if error}
				<div class="py-4 sm:grid sm:grid-cols-3 sm:gap-4 sm:py-5">
					<dt class="text-sm font-medium text-muted-foreground">
						{m.dashboard_clientSideAuth_errorDetails()}
					</dt>
					<dd class="mt-1 text-sm text-destructive sm:col-span-2 sm:mt-0">
						{error()}
					</dd>
				</div>
			{/if}
		</dl>
	</Card.Content>
</Card.Root>
