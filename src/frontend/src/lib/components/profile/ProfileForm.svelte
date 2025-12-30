<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Textarea } from '$lib/components/ui/textarea';
	import { Label } from '$lib/components/ui/label';
	import { PhoneInput } from '$lib/components/ui/phone-input';
	import { ProfileHeader } from '$lib/components/profile';
	import type { User } from '$lib/types';
	import * as m from '$lib/paraglide/messages';
	import { browserClient } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { invalidateAll } from '$app/navigation';
	import { isValidationProblemDetails, mapFieldErrors, getErrorMessage } from '$lib/api';
	import { createFieldShakes } from '$lib/state';

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

	// Field-level errors from backend validation
	let fieldErrors = $state<Record<string, string>>({});

	// Field-level shake animation for error feedback
	const fieldShakes = createFieldShakes();

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
		fieldErrors = {};

		try {
			const { response, error: apiError } = await browserClient.PATCH('/api/users/me', {
				body: {
					firstName: firstName.trim() || null,
					lastName: lastName.trim() || null,
					phoneNumber: phoneNumber.trim() || null,
					bio: bio.trim() || null
				}
			});

			if (response.ok) {
				toast.success(m.profile_personalInfo_updateSuccess());
				await invalidateAll();
			} else if (isValidationProblemDetails(apiError)) {
				fieldErrors = mapFieldErrors(apiError.errors);
				fieldShakes.triggerFields(Object.keys(fieldErrors));
				toast.error(apiError.title || m.profile_personalInfo_updateError());
			} else {
				toast.error(m.profile_personalInfo_updateError(), {
					description: getErrorMessage(apiError, m.profile_personalInfo_updateError())
				});
			}
		} catch {
			toast.error(m.profile_personalInfo_updateError());
		} finally {
			isLoading = false;
		}
	}
</script>

<Card.Root class="card-hover">
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
							class={fieldShakes.class('firstName')}
							aria-invalid={!!fieldErrors.firstName}
							aria-describedby={fieldErrors.firstName ? 'firstName-error' : undefined}
						/>
						{#if fieldErrors.firstName}
							<p id="firstName-error" class="text-xs text-destructive">{fieldErrors.firstName}</p>
						{/if}
					</div>
					<div class="grid gap-2">
						<Label for="lastName">{m.profile_personalInfo_lastName()}</Label>
						<Input
							id="lastName"
							autocomplete="family-name"
							bind:value={lastName}
							placeholder={m.profile_personalInfo_lastNamePlaceholder()}
							class={fieldShakes.class('lastName')}
							aria-invalid={!!fieldErrors.lastName}
							aria-describedby={fieldErrors.lastName ? 'lastName-error' : undefined}
						/>
						{#if fieldErrors.lastName}
							<p id="lastName-error" class="text-xs text-destructive">{fieldErrors.lastName}</p>
						{/if}
					</div>
				</div>

				<div class="grid gap-2">
					<Label for="phoneNumber">{m.profile_personalInfo_phoneNumber()}</Label>
					<PhoneInput
						id="phoneNumber"
						bind:value={phoneNumber}
						placeholder="123 456 789"
						class={fieldShakes.class('phoneNumber')}
						aria-invalid={!!fieldErrors.phoneNumber}
						aria-describedby={fieldErrors.phoneNumber ? 'phoneNumber-error' : undefined}
					/>
					{#if fieldErrors.phoneNumber}
						<p id="phoneNumber-error" class="text-xs text-destructive">{fieldErrors.phoneNumber}</p>
					{/if}
				</div>

				<div class="grid gap-2">
					<Label for="bio">{m.profile_personalInfo_bio()}</Label>
					<Textarea
						id="bio"
						bind:value={bio}
						placeholder={m.profile_personalInfo_bioPlaceholder()}
						class={fieldShakes.class('bio')}
						aria-invalid={!!fieldErrors.bio}
						aria-describedby={fieldErrors.bio ? 'bio-error' : undefined}
					/>
					{#if fieldErrors.bio}
						<p id="bio-error" class="text-xs text-destructive">{fieldErrors.bio}</p>
					{/if}
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
