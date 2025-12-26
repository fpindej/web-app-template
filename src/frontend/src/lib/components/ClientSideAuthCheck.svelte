<script lang="ts">
	import { browserClient } from '$lib/api/client';
	import type { components } from '$lib/api/v1';
	import { onMount } from 'svelte';
	import * as Card from '$lib/components/ui/card';
	import { Badge } from '$lib/components/ui/badge';
	import { t } from '$lib/i18n';
	import type { TranslationKey } from '$lib/types/i18n';

	let clientUser = $state<components['schemas']['MeResponse'] | null>(null);
	let loading = $state(true);
	let error = $state<TranslationKey | ''>('');

	onMount(async () => {
		try {
			const { data, response } = await browserClient.GET('/api/auth/me');
			if (response.ok && data) {
				clientUser = data;
			} else {
				error = 'dashboard.clientSideAuth.failedFetch';
			}
		} catch {
			error = 'dashboard.clientSideAuth.errorFetch';
		} finally {
			loading = false;
		}
	});
</script>

<Card.Root>
	<Card.Header>
		<Card.Title class="text-lg">{$t('dashboard.clientSideAuth.title')}</Card.Title>
		<Card.Description>{$t('dashboard.clientSideAuth.description')}</Card.Description>
	</Card.Header>
	<Card.Content>
		<dl class="sm:divide-y sm:divide-border">
			<div class="py-4 sm:grid sm:grid-cols-3 sm:gap-4 sm:py-5">
				<dt class="text-sm font-medium text-muted-foreground">{$t('dashboard.status')}</dt>
				<dd class="mt-1 text-sm text-foreground sm:col-span-2 sm:mt-0">
					{#if loading}
						<Badge variant="warning">{$t('dashboard.clientSideAuth.loading')}</Badge>
					{:else if error}
						<Badge variant="destructive">{$t('dashboard.clientSideAuth.error')}</Badge>
					{:else}
						<Badge variant="success">{$t('dashboard.clientSideAuth.authenticated')}</Badge>
					{/if}
				</dd>
			</div>
			{#if clientUser}
				<div class="py-4 sm:grid sm:grid-cols-3 sm:gap-4 sm:py-5">
					<dt class="text-sm font-medium text-muted-foreground">
						{$t('dashboard.clientSideAuth.usernameClient')}
					</dt>
					<dd class="mt-1 text-sm text-foreground sm:col-span-2 sm:mt-0">{clientUser.username}</dd>
				</div>
				<div class="py-4 sm:grid sm:grid-cols-3 sm:gap-4 sm:py-5">
					<dt class="text-sm font-medium text-muted-foreground">{$t('dashboard.roles')}</dt>
					<dd class="mt-1 text-sm text-foreground sm:col-span-2 sm:mt-0">
						{clientUser.roles?.join(', ') || $t('dashboard.none')}
					</dd>
				</div>
			{/if}
			{#if error}
				<div class="py-4 sm:grid sm:grid-cols-3 sm:gap-4 sm:py-5">
					<dt class="text-sm font-medium text-muted-foreground">
						{$t('dashboard.clientSideAuth.errorDetails')}
					</dt>
					<dd class="mt-1 text-sm text-destructive sm:col-span-2 sm:mt-0">
						{#if error}{$t(error)}{/if}
					</dd>
				</div>
			{/if}
		</dl>
	</Card.Content>
</Card.Root>
