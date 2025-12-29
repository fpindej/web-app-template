<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Textarea } from '$lib/components/ui/textarea';
	import { Label } from '$lib/components/ui/label';
	import * as Avatar from '$lib/components/ui/avatar';
	import type { User } from '$lib/types';
	import * as m from '$lib/paraglide/messages';
	import { browserClient } from '$lib/api/client';
	import { toast } from 'svelte-sonner';
	import { invalidateAll } from '$app/navigation';

	let { user }: { user: User | null | undefined } = $props();

	// Form state - synced with user prop changes
	let firstName = $state('');
	let lastName = $state('');
	let bio = $state('');
	let isLoading = $state(false);

	// Sync form state when user prop changes (e.g., after invalidateAll)
	$effect(() => {
		firstName = user?.firstName ?? '';
		lastName = user?.lastName ?? '';
		bio = user?.bio ?? '';
	});

	// Computed display name
	const displayName = $derived.by(() => {
		if (firstName || lastName) {
			return [firstName, lastName].filter(Boolean).join(' ');
		}
		return user?.username ?? m.common_user();
	});

	// Computed initials for avatar
	const initials = $derived.by(() => {
		if (firstName && lastName) {
			return `${firstName[0]}${lastName[0]}`.toUpperCase();
		}
		if (firstName) {
			return firstName.substring(0, 2).toUpperCase();
		}
		return user?.username?.substring(0, 2).toUpperCase() ?? 'ME';
	});

	async function handleSubmit(e: Event) {
		e.preventDefault();
		isLoading = true;

		try {
			const { response, error: apiError } = await browserClient.PATCH('/api/auth/profile', {
				body: {
					firstName: firstName || null,
					lastName: lastName || null,
					bio: bio || null
				}
			});

			if (response.ok) {
				toast.success(m.profile_personalInfo_updateSuccess());
				await invalidateAll();
			} else {
				const errorMessage =
					apiError?.detail || apiError?.title || m.profile_personalInfo_updateError();
				toast.error(m.profile_personalInfo_updateError(), { description: errorMessage });
			}
		} catch {
			toast.error(m.profile_personalInfo_updateError());
		} finally {
			isLoading = false;
		}
	}
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>{m.profile_personalInfo_title()}</Card.Title>
		<Card.Description>{m.profile_personalInfo_description()}</Card.Description>
	</Card.Header>
	<Card.Content>
		<form onsubmit={handleSubmit} class="space-y-6">
			<div class="flex flex-col items-center gap-4 sm:flex-row">
				<div class="relative h-24 w-24">
					<Avatar.Root class="h-24 w-24">
						{#if user?.avatarUrl}
							<Avatar.Image src={user.avatarUrl} alt={displayName} />
						{/if}
						<Avatar.Fallback class="text-lg">
							{initials}
						</Avatar.Fallback>
					</Avatar.Root>
				</div>
				<div class="flex flex-col gap-1 text-center sm:text-start">
					<h3 class="text-lg font-medium">{displayName}</h3>
					<p class="text-sm text-muted-foreground">{user?.email ?? ''}</p>
					<Button variant="outline" size="sm" class="mt-2 w-full sm:w-auto">
						{m.profile_personalInfo_changeAvatar()}
					</Button>
				</div>
			</div>

			<div class="grid gap-4">
				<div class="grid gap-2">
					<Label for="email">{m.profile_personalInfo_email()}</Label>
					<Input id="email" type="email" autocomplete="email" value={user?.email} disabled />
					<p class="text-xs text-muted-foreground">
						{m.profile_personalInfo_emailDescription()}
					</p>
				</div>

				<div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
					<div class="grid gap-2">
						<Label for="firstName">{m.profile_personalInfo_firstName()}</Label>
						<Input
							id="firstName"
							autocomplete="given-name"
							bind:value={firstName}
							placeholder={m.profile_personalInfo_firstNamePlaceholder()}
						/>
					</div>
					<div class="grid gap-2">
						<Label for="lastName">{m.profile_personalInfo_lastName()}</Label>
						<Input
							id="lastName"
							autocomplete="family-name"
							bind:value={lastName}
							placeholder={m.profile_personalInfo_lastNamePlaceholder()}
						/>
					</div>
				</div>

				<div class="grid gap-2">
					<Label for="bio">{m.profile_personalInfo_bio()}</Label>
					<Textarea
						id="bio"
						bind:value={bio}
						placeholder={m.profile_personalInfo_bioPlaceholder()}
					/>
				</div>

				<div class="flex justify-end">
					<Button type="submit" disabled={isLoading}>
						{isLoading ? m.profile_personalInfo_saving() : m.profile_personalInfo_saveChanges()}
					</Button>
				</div>
			</div>
		</form>
	</Card.Content>
</Card.Root>
