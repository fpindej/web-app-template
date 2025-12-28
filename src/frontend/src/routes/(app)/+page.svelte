<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import * as m from '$lib/paraglide/messages';
	import { ShieldCheck, Globe, Layout, FileCode } from 'lucide-svelte';

	let { data } = $props();

	const features = [
		{
			title: m.dashboard_feature_auth_title,
			desc: m.dashboard_feature_auth_desc,
			icon: ShieldCheck,
			color: 'text-blue-500'
		},
		{
			title: m.dashboard_feature_i18n_title,
			desc: m.dashboard_feature_i18n_desc,
			icon: Globe,
			color: 'text-green-500'
		},
		{
			title: m.dashboard_feature_ui_title,
			desc: m.dashboard_feature_ui_desc,
			icon: Layout,
			color: 'text-purple-500'
		},
		{
			title: m.dashboard_feature_api_title,
			desc: m.dashboard_feature_api_desc,
			icon: FileCode,
			color: 'text-orange-500'
		}
	];

	const steps = [
		m.dashboard_guide_step1,
		m.dashboard_guide_step2,
		m.dashboard_guide_step3,
		m.dashboard_guide_step4
	];
</script>

<svelte:head>
	<title>{m.common_meta_titleTemplate(m.common_meta_dashboard_title())}</title>
	<meta name="description" content={m.common_meta_dashboard_description()} />
</svelte:head>

<div class="space-y-8">
	<!-- Welcome Section -->
	<div class="flex flex-col gap-2">
		<h1 class="text-3xl font-bold tracking-tight">{m.dashboard_welcome_title()}</h1>
		<p class="text-lg text-muted-foreground">
			{m.dashboard_welcome_description()}
		</p>
	</div>

	<!-- Features Grid -->
	<div class="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
		{#each features as feature (feature.title)}
			<Card.Root>
				<Card.Header class="flex flex-row items-center justify-between space-y-0 pb-2">
					<Card.Title class="text-sm font-medium">{feature.title()}</Card.Title>
					<feature.icon class="h-4 w-4 {feature.color}" />
				</Card.Header>
				<Card.Content>
					<p class="text-xs text-muted-foreground">
						{feature.desc()}
					</p>
				</Card.Content>
			</Card.Root>
		{/each}
	</div>

	<!-- Getting Started Guide -->
	<Card.Root>
		<Card.Header>
			<Card.Title>{m.dashboard_guide_title()}</Card.Title>
		</Card.Header>
		<Card.Content>
			<div class="grid gap-4 md:grid-cols-2">
				<div class="space-y-4">
					{#each steps as step, i (i)}
						<div class="flex items-start gap-3">
							<div
								class="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-primary text-xs font-bold text-primary-foreground"
							>
								{i + 1}
							</div>
							<p class="text-sm text-muted-foreground">
								{step()}
							</p>
						</div>
					{/each}
				</div>
				<div class="rounded-lg border bg-muted/50 p-4">
					<div class="flex items-center gap-2 text-sm font-medium">
						<ShieldCheck class="h-4 w-4 text-green-500" />
						<span>{m.dashboard_serverSideAuth_authenticated()}</span>
					</div>
					<div class="mt-2 text-xs text-muted-foreground">
						Logged in as <span class="font-mono text-foreground">{data.user.username}</span>
					</div>
				</div>
			</div>
		</Card.Content>
	</Card.Root>
</div>
