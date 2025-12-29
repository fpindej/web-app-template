<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Textarea } from '$lib/components/ui/textarea';
	import { Label } from '$lib/components/ui/label';
	import { ProfileHeader } from '$lib/components/profile';
	import type { User } from '$lib/types';
	import * as m from '$lib/paraglide/messages';
	import { browserClient } from '$lib/api/client';
	import { toast } from 'svelte-sonner';
	import { invalidateAll } from '$app/navigation';

	interface Props {
		user: User | null | undefined;
	}

	let { user }: Props = $props();

	// Form state
	let firstName = $state('');
	let lastName = $state('');
	let phoneNumber = $state('');
	let bio = $state('');
	let isLoading = $state(false);

	// Sync form state when user prop changes (e.g., after invalidateAll)
	$effect(() => {
		firstName = user?.firstName ?? '';
		lastName = user?.lastName ?? '';
		phoneNumber = user?.phoneNumber ?? '';
		bio = user?.bio ?? '';
	});

	async function handleSubmit(e: Event) {
		e.preventDefault();
		isLoading = true;

		try {
			const { response, error: apiError } = await browserClient.PATCH('/api/users/me', {
				body: {
					firstName: firstName || null,
					lastName: lastName || null,
					phoneNumber: phoneNumber || null,
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
			<ProfileHeader {user} />

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
					<Label for="phoneNumber">{m.profile_personalInfo_phoneNumber()}</Label>
					<Input
						id="phoneNumber"
						type="tel"
						autocomplete="tel"
						bind:value={phoneNumber}
						placeholder={m.profile_personalInfo_phoneNumberPlaceholder()}
					/>
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
