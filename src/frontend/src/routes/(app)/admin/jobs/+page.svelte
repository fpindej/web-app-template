<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import * as Dialog from '$lib/components/ui/dialog';
	import { JobTable } from '$lib/components/admin';
	import { browserClient, getErrorMessage, isRateLimited, getRetryAfterSeconds } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { invalidateAll } from '$app/navigation';
	import { createCooldown } from '$lib/state';
	import { hasPermission, Permissions } from '$lib/utils';
	import { RefreshCw, Loader2 } from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();

	let canManageJobs = $derived(hasPermission(data.user, Permissions.Jobs.Manage));
	let isRestoring = $state(false);
	let restoreDialogOpen = $state(false);
	const cooldown = createCooldown();

	async function restoreJobs() {
		isRestoring = true;
		const { response, error } = await browserClient.POST('/api/v1/admin/jobs/restore');
		isRestoring = false;
		restoreDialogOpen = false;

		if (response.ok) {
			toast.success(m.admin_jobs_restoreSuccess());
			await invalidateAll();
		} else if (isRateLimited(response)) {
			const retryAfter = getRetryAfterSeconds(response);
			if (retryAfter) cooldown.start(retryAfter);
			toast.error(m.error_rateLimited(), {
				description: retryAfter
					? m.error_rateLimitedDescriptionWithRetry({ seconds: retryAfter })
					: m.error_rateLimitedDescription()
			});
		} else {
			toast.error(getErrorMessage(error, m.admin_jobs_restoreError()));
		}
	}
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_adminJobs_title() })}</title>
	<meta name="description" content={m.meta_adminJobs_description()} />
</svelte:head>

<div class="space-y-6">
	<div class="flex items-center justify-between">
		<div>
			<h3 class="text-lg font-medium">{m.admin_jobs_title()}</h3>
			<p class="text-sm text-muted-foreground">{m.admin_jobs_description()}</p>
		</div>
		{#if canManageJobs}
			<Dialog.Root bind:open={restoreDialogOpen}>
				<Dialog.Trigger>
					{#snippet child({ props })}
						<Button variant="outline" size="sm" {...props}>
							<RefreshCw class="me-2 h-4 w-4" />
							{m.admin_jobs_restore()}
						</Button>
					{/snippet}
				</Dialog.Trigger>
				<Dialog.Content>
					<Dialog.Header>
						<Dialog.Title>{m.admin_jobs_restore()}</Dialog.Title>
						<Dialog.Description>{m.admin_jobs_restoreConfirm()}</Dialog.Description>
					</Dialog.Header>
					<Dialog.Footer class="flex-col-reverse sm:flex-row">
						<Button variant="outline" onclick={() => (restoreDialogOpen = false)}>
							{m.common_cancel()}
						</Button>
						<Button disabled={isRestoring || cooldown.active} onclick={restoreJobs}>
							{#if cooldown.active}
								{m.common_waitSeconds({ seconds: cooldown.remaining })}
							{:else}
								{#if isRestoring}
									<Loader2 class="me-2 h-4 w-4 animate-spin" />
								{/if}
								{m.admin_jobs_restore()}
							{/if}
						</Button>
					</Dialog.Footer>
				</Dialog.Content>
			</Dialog.Root>
		{/if}
	</div>
	<div class="h-px w-full bg-border"></div>

	<Card.Root>
		<Card.Content class="p-0">
			<JobTable jobs={data.jobs} />
		</Card.Content>
	</Card.Root>
</div>
