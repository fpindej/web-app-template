<script lang="ts">
	import * as Dialog from '$lib/components/ui/dialog';
	import * as Avatar from '$lib/components/ui/avatar';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import * as m from '$lib/paraglide/messages';
	import { browserClient } from '$lib/api/client';
	import { toast } from 'svelte-sonner';
	import { invalidateAll } from '$app/navigation';

	interface Props {
		open: boolean;
		currentAvatarUrl: string | null | undefined;
		displayName: string;
		initials: string;
	}

	let { open = $bindable(), currentAvatarUrl, displayName, initials }: Props = $props();

	let avatarUrl = $state('');
	let avatarUrlError = $state('');
	let isLoading = $state(false);

	// Sync avatarUrl when dialog opens or currentAvatarUrl changes
	$effect(() => {
		if (open) {
			avatarUrl = currentAvatarUrl ?? '';
			avatarUrlError = '';
		}
	});

	/**
	 * Validates a URL string for avatar usage.
	 * Accepts http/https URLs.
	 */
	function isValidAvatarUrl(url: string): boolean {
		if (!url.trim()) return true; // Empty is valid (clears avatar)

		try {
			const parsed = new URL(url);
			return ['http:', 'https:'].includes(parsed.protocol);
		} catch {
			return false;
		}
	}

	function handleUrlChange(value: string) {
		avatarUrl = value;
		if (value && !isValidAvatarUrl(value)) {
			avatarUrlError = m.profile_personalInfo_avatarUrlInvalid();
		} else {
			avatarUrlError = '';
		}
	}

	async function handleSubmit() {
		if (avatarUrl && !isValidAvatarUrl(avatarUrl)) {
			avatarUrlError = m.profile_personalInfo_avatarUrlInvalid();
			return;
		}

		isLoading = true;

		try {
			const { response, error: apiError } = await browserClient.PATCH('/api/users/me', {
				body: {
					avatarUrl: avatarUrl || null
				}
			});

			if (response.ok) {
				toast.success(m.profile_personalInfo_avatarUpdateSuccess());
				open = false;
				await invalidateAll();
			} else {
				const errorMessage =
					apiError?.detail || apiError?.title || m.profile_personalInfo_avatarUpdateError();
				toast.error(m.profile_personalInfo_avatarUpdateError(), { description: errorMessage });
			}
		} catch {
			toast.error(m.profile_personalInfo_avatarUpdateError());
		} finally {
			isLoading = false;
		}
	}
</script>

<Dialog.Root bind:open>
	<Dialog.Trigger>
		{#snippet child({ props })}
			<Button {...props} variant="outline" size="sm" class="mt-2 w-full sm:w-auto">
				{m.profile_personalInfo_changeAvatar()}
			</Button>
		{/snippet}
	</Dialog.Trigger>
	<Dialog.Content class="sm:max-w-md">
		<Dialog.Header>
			<Dialog.Title>{m.profile_personalInfo_avatarDialogTitle()}</Dialog.Title>
			<Dialog.Description>
				{m.profile_personalInfo_avatarDialogDescription()}
			</Dialog.Description>
		</Dialog.Header>
		<div class="grid gap-4 py-4">
			<div class="flex justify-center">
				<Avatar.Root class="h-24 w-24">
					{#if avatarUrl && isValidAvatarUrl(avatarUrl)}
						<Avatar.Image src={avatarUrl} alt={displayName} />
					{/if}
					<Avatar.Fallback class="text-lg">
						{initials}
					</Avatar.Fallback>
				</Avatar.Root>
			</div>
			<div class="grid gap-2">
				<Label for="avatarUrl">{m.profile_personalInfo_avatarUrl()}</Label>
				<Input
					id="avatarUrl"
					type="url"
					value={avatarUrl}
					oninput={(e) => handleUrlChange(e.currentTarget.value)}
					placeholder={m.profile_personalInfo_avatarUrlPlaceholder()}
				/>
				{#if avatarUrlError}
					<p class="text-xs text-destructive">{avatarUrlError}</p>
				{:else}
					<p class="text-xs text-muted-foreground">
						{m.profile_personalInfo_avatarUrlHint()}
					</p>
				{/if}
			</div>
		</div>
		<Dialog.Footer>
			<Dialog.Close>
				{#snippet child({ props })}
					<Button {...props} variant="outline">
						{m.profile_personalInfo_avatarCancel()}
					</Button>
				{/snippet}
			</Dialog.Close>
			<Button onclick={handleSubmit} disabled={isLoading || !!avatarUrlError}>
				{isLoading ? m.profile_personalInfo_saving() : m.profile_personalInfo_avatarSave()}
			</Button>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>
