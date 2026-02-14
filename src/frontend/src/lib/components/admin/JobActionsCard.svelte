<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import * as Dialog from '$lib/components/ui/dialog';
	import { browserClient, getErrorMessage, isRateLimited, getRetryAfterSeconds } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { goto, invalidateAll } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { createCooldown } from '$lib/state';
	import { Play, Pause, RotateCcw, Trash2, Loader2 } from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		jobId: string;
		isPaused: boolean;
	}

	let { jobId, isPaused }: Props = $props();

	let isTriggering = $state(false);
	let isPausing = $state(false);
	let isResuming = $state(false);
	let isDeleting = $state(false);
	let triggerDialogOpen = $state(false);
	let deleteDialogOpen = $state(false);
	const cooldown = createCooldown();

	function handleRateLimited(response: Response) {
		const retryAfter = getRetryAfterSeconds(response);
		if (retryAfter) cooldown.start(retryAfter);
		toast.error(m.error_rateLimited(), {
			description: retryAfter
				? m.error_rateLimitedDescriptionWithRetry({ seconds: retryAfter })
				: m.error_rateLimitedDescription()
		});
	}

	async function triggerJob() {
		isTriggering = true;
		const { response, error } = await browserClient.POST('/api/v1/admin/jobs/{jobId}/trigger', {
			params: { path: { jobId } }
		});
		isTriggering = false;
		triggerDialogOpen = false;

		if (response.ok) {
			toast.success(m.admin_jobDetail_triggerSuccess());
			await invalidateAll();
		} else if (isRateLimited(response)) {
			handleRateLimited(response);
		} else {
			toast.error(getErrorMessage(error, m.admin_jobDetail_triggerError()));
		}
	}

	async function pauseJob() {
		isPausing = true;
		const { response, error } = await browserClient.POST('/api/v1/admin/jobs/{jobId}/pause', {
			params: { path: { jobId } }
		});
		isPausing = false;

		if (response.ok) {
			toast.success(m.admin_jobDetail_pauseSuccess());
			await invalidateAll();
		} else if (isRateLimited(response)) {
			handleRateLimited(response);
		} else {
			toast.error(getErrorMessage(error, m.admin_jobDetail_pauseError()));
		}
	}

	async function resumeJob() {
		isResuming = true;
		const { response, error } = await browserClient.POST('/api/v1/admin/jobs/{jobId}/resume', {
			params: { path: { jobId } }
		});
		isResuming = false;

		if (response.ok) {
			toast.success(m.admin_jobDetail_resumeSuccess());
			await invalidateAll();
		} else if (isRateLimited(response)) {
			handleRateLimited(response);
		} else {
			toast.error(getErrorMessage(error, m.admin_jobDetail_resumeError()));
		}
	}

	async function deleteJob() {
		isDeleting = true;
		const { response, error } = await browserClient.DELETE('/api/v1/admin/jobs/{jobId}', {
			params: { path: { jobId } }
		});
		isDeleting = false;
		deleteDialogOpen = false;

		if (response.ok) {
			toast.success(m.admin_jobDetail_deleteSuccess());
			await goto(resolve('/admin/jobs'));
		} else if (isRateLimited(response)) {
			handleRateLimited(response);
		} else {
			toast.error(getErrorMessage(error, m.admin_jobDetail_deleteError()));
		}
	}
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>{m.admin_jobDetail_actions()}</Card.Title>
		<Card.Description>{m.admin_jobDetail_actionsDescription()}</Card.Description>
	</Card.Header>
	<Card.Content class="flex flex-wrap gap-2">
		<!-- Trigger -->
		<Dialog.Root bind:open={triggerDialogOpen}>
			<Dialog.Trigger>
				{#snippet child({ props })}
					<Button variant="outline" size="sm" {...props}>
						<Play class="me-2 h-4 w-4" />
						{m.admin_jobDetail_trigger()}
					</Button>
				{/snippet}
			</Dialog.Trigger>
			<Dialog.Content>
				<Dialog.Header>
					<Dialog.Title>{m.admin_jobDetail_trigger()}</Dialog.Title>
					<Dialog.Description>{m.admin_jobDetail_triggerConfirm()}</Dialog.Description>
				</Dialog.Header>
				<Dialog.Footer class="flex-col-reverse sm:flex-row">
					<Button variant="outline" onclick={() => (triggerDialogOpen = false)}>
						{m.common_cancel()}
					</Button>
					<Button disabled={isTriggering || cooldown.active} onclick={triggerJob}>
						{#if isTriggering}
							<Loader2 class="me-2 h-4 w-4 animate-spin" />
						{/if}
						{m.admin_jobDetail_trigger()}
					</Button>
				</Dialog.Footer>
			</Dialog.Content>
		</Dialog.Root>

		<!-- Pause / Resume -->
		{#if isPaused}
			<Button
				variant="outline"
				size="sm"
				disabled={isResuming || cooldown.active}
				onclick={resumeJob}
			>
				{#if isResuming}
					<Loader2 class="me-2 h-4 w-4 animate-spin" />
				{:else}
					<RotateCcw class="me-2 h-4 w-4" />
				{/if}
				{m.admin_jobDetail_resume()}
			</Button>
		{:else}
			<Button
				variant="outline"
				size="sm"
				disabled={isPausing || cooldown.active}
				onclick={pauseJob}
			>
				{#if isPausing}
					<Loader2 class="me-2 h-4 w-4 animate-spin" />
				{:else}
					<Pause class="me-2 h-4 w-4" />
				{/if}
				{m.admin_jobDetail_pause()}
			</Button>
		{/if}

		<!-- Delete -->
		<Dialog.Root bind:open={deleteDialogOpen}>
			<Dialog.Trigger>
				{#snippet child({ props })}
					<Button variant="destructive" size="sm" {...props}>
						<Trash2 class="me-2 h-4 w-4" />
						{m.admin_jobDetail_delete()}
					</Button>
				{/snippet}
			</Dialog.Trigger>
			<Dialog.Content>
				<Dialog.Header>
					<Dialog.Title>{m.admin_jobDetail_delete()}</Dialog.Title>
					<Dialog.Description>{m.admin_jobDetail_deleteConfirm()}</Dialog.Description>
				</Dialog.Header>
				<Dialog.Footer class="flex-col-reverse sm:flex-row">
					<Button variant="outline" onclick={() => (deleteDialogOpen = false)}>
						{m.common_cancel()}
					</Button>
					<Button
						variant="destructive"
						disabled={isDeleting || cooldown.active}
						onclick={deleteJob}
					>
						{#if isDeleting}
							<Loader2 class="me-2 h-4 w-4 animate-spin" />
						{/if}
						{m.common_delete()}
					</Button>
				</Dialog.Footer>
			</Dialog.Content>
		</Dialog.Root>
	</Card.Content>
</Card.Root>
