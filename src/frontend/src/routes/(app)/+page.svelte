<script lang="ts">
	import { ClientSideAuthCheck } from '$lib/components/auth';
	import * as Card from '$lib/components/ui/card';
	import { Badge } from '$lib/components/ui/badge';
	import * as m from '$lib/paraglide/messages';

	let { data } = $props();
</script>

<svelte:head>
	<title>{m.common_meta_titleTemplate().replace('%s', m.common_meta_dashboard_title())}</title>
	<meta name="description" content={m.common_meta_dashboard_description()} />
</svelte:head>

<div class="md:flex md:items-center md:justify-between">
	<div class="min-w-0 flex-1">
		<h2
			class="text-2xl leading-7 font-bold text-foreground sm:truncate sm:text-3xl sm:tracking-tight"
		>
			{m.dashboard_title()}
		</h2>
	</div>
</div>

<div class="mt-8 grid grid-cols-1 gap-6 lg:grid-cols-2">
	<!-- Server Side Auth Card -->
	<Card.Root>
		<Card.Header>
			<Card.Title class="text-lg">{m.dashboard_serverSideAuth_title()}</Card.Title>
			<Card.Description>{m.dashboard_serverSideAuth_description()}</Card.Description>
		</Card.Header>
		<Card.Content>
			<dl class="sm:divide-y sm:divide-border">
				<div class="py-4 sm:grid sm:grid-cols-3 sm:gap-4 sm:py-5">
					<dt class="text-sm font-medium text-muted-foreground">{m.dashboard_status()}</dt>
					<dd class="mt-1 text-sm text-foreground sm:col-span-2 sm:mt-0">
						<Badge variant="success">{m.dashboard_serverSideAuth_authenticated()}</Badge>
					</dd>
				</div>
				<div class="py-4 sm:grid sm:grid-cols-3 sm:gap-4 sm:py-5">
					<dt class="text-sm font-medium text-muted-foreground">{m.dashboard_username()}</dt>
					<dd class="mt-1 text-sm text-foreground sm:col-span-2 sm:mt-0">{data.user.username}</dd>
				</div>
				<div class="py-4 sm:grid sm:grid-cols-3 sm:gap-4 sm:py-5">
					<dt class="text-sm font-medium text-muted-foreground">{m.dashboard_roles()}</dt>
					<dd class="mt-1 text-sm text-foreground sm:col-span-2 sm:mt-0">
						{data.user.roles?.join(', ') || m.dashboard_none()}
					</dd>
				</div>
			</dl>
		</Card.Content>
	</Card.Root>

	<!-- Client Side Auth Card -->
	<ClientSideAuthCheck />
</div>
