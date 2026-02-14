<script lang="ts">
	import { resolve } from '$app/paths';
	import { buttonVariants } from '$lib/components/ui/button';
	import { JobInfoCard, JobActionsCard, JobExecutionHistory } from '$lib/components/admin';
	import { ArrowLeft } from '@lucide/svelte';
	import { hasPermission, Permissions, cn } from '$lib/utils';
	import * as m from '$lib/paraglide/messages';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();

	let canManageJobs = $derived(hasPermission(data.user, Permissions.Jobs.Manage));
</script>

<!-- eslint-disable svelte/no-navigation-without-resolve -- hrefs are pre-resolved using resolve() -->
<svelte:head>
	<title>{m.meta_titleTemplate({ title: data.job?.id ?? m.meta_adminJobDetail_title() })}</title>
	<meta name="description" content={m.meta_adminJobDetail_description()} />
</svelte:head>

<div class="space-y-6">
	<div class="flex items-center gap-4">
		<a
			href={resolve('/admin/jobs')}
			class={cn(buttonVariants({ variant: 'ghost', size: 'icon' }), 'h-10 w-10')}
			aria-label={m.admin_jobDetail_back()}
		>
			<ArrowLeft class="h-4 w-4" />
		</a>
		<div>
			<h3 class="text-lg font-medium">{data.job?.id}</h3>
			<p class="font-mono text-sm text-muted-foreground">{data.job?.cron}</p>
		</div>
	</div>
	<div class="h-px w-full bg-border"></div>

	{#if data.job}
		<div class="grid gap-6 xl:grid-cols-2">
			<JobInfoCard job={data.job} />
			{#if canManageJobs}
				<JobActionsCard jobId={data.job.id ?? ''} isPaused={data.job.isPaused ?? false} />
			{/if}
		</div>

		<JobExecutionHistory executions={data.job.executionHistory ?? []} />
	{/if}
</div>
