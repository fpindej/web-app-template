<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { JobTable } from '$lib/components/admin';
	import * as m from '$lib/paraglide/messages';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_adminJobs_title() })}</title>
	<meta name="description" content={m.meta_adminJobs_description()} />
</svelte:head>

<div class="space-y-6">
	<div>
		<h3 class="text-lg font-medium">{m.admin_jobs_title()}</h3>
		<p class="text-sm text-muted-foreground">{m.admin_jobs_description()}</p>
	</div>
	<div class="h-px w-full bg-border"></div>

	<Card.Root>
		<Card.Content class="p-0">
			<JobTable jobs={data.jobs} />
		</Card.Content>
	</Card.Root>
</div>
