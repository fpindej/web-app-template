<script lang="ts">
	import ClientSideAuthCheck from '$lib/components/ClientSideAuthCheck.svelte';
	import * as Card from '$lib/components/ui/card';
	import { Badge } from '$lib/components/ui/badge';
	import { t } from '$lib/i18n';

	let { data } = $props();
</script>

<div class="md:flex md:items-center md:justify-between">
	<div class="min-w-0 flex-1">
		<h2
			class="text-2xl leading-7 font-bold text-foreground sm:truncate sm:text-3xl sm:tracking-tight"
		>
			{$t('dashboard.title')}
		</h2>
		<p class="mt-1 text-sm text-muted-foreground">
			{$t('dashboard.currentDate', {
				date: new Date()
			})}
		</p>
	</div>
</div>

<div class="mt-8 grid grid-cols-1 gap-6 lg:grid-cols-2">
	<!-- Server Side Auth Card -->
	<Card.Root>
		<Card.Header>
			<Card.Title class="text-lg">{$t('dashboard.serverSideAuth.title')}</Card.Title>
			<Card.Description>{$t('dashboard.serverSideAuth.description')}</Card.Description>
		</Card.Header>
		<Card.Content>
			<dl class="sm:divide-y sm:divide-border">
				<div class="py-4 sm:grid sm:grid-cols-3 sm:gap-4 sm:py-5">
					<dt class="text-sm font-medium text-muted-foreground">{$t('dashboard.status')}</dt>
					<dd class="mt-1 text-sm text-foreground sm:col-span-2 sm:mt-0">
						<Badge variant="success">{$t('dashboard.clientSideAuth.authenticated')}</Badge>
					</dd>
				</div>
				<div class="py-4 sm:grid sm:grid-cols-3 sm:gap-4 sm:py-5">
					<dt class="text-sm font-medium text-muted-foreground">{$t('dashboard.username')}</dt>
					<dd class="mt-1 text-sm text-foreground sm:col-span-2 sm:mt-0">{data.user.username}</dd>
				</div>
				<div class="py-4 sm:grid sm:grid-cols-3 sm:gap-4 sm:py-5">
					<dt class="text-sm font-medium text-muted-foreground">{$t('dashboard.roles')}</dt>
					<dd class="mt-1 text-sm text-foreground sm:col-span-2 sm:mt-0">
						{data.user.roles?.join(', ') || $t('dashboard.none')}
					</dd>
				</div>
			</dl>
		</Card.Content>
	</Card.Root>

	<!-- Client Side Auth Card -->
	<ClientSideAuthCheck />
</div>
