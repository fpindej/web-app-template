<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Textarea } from '$lib/components/ui/textarea';
	import { Label } from '$lib/components/ui/label';
	import * as Avatar from '$lib/components/ui/avatar';
	import type { components } from '$lib/api/v1';
	import { t } from '$lib/i18n';

	type UserType = components['schemas']['MeResponse'];

	let { user }: { user: UserType | null | undefined } = $props();

	// Mock data for placeholders
	let fullName = $state('John Doe');
	let email = $state('john.doe@example.com');
	let bio = $state('Software Engineer based in San Francisco.');
	let isLoading = $state(false);

	function handleSubmit(e: Event) {
		e.preventDefault();
		isLoading = true;
		// TODO: Implement actual API call to update profile
		setTimeout(() => {
			isLoading = false;
		}, 1000);
	}
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>{$t('profile.personalInfo.title')}</Card.Title>
		<Card.Description>{$t('profile.personalInfo.description')}</Card.Description>
	</Card.Header>
	<Card.Content>
		<form onsubmit={handleSubmit} class="space-y-6">
			<div class="flex flex-col items-center gap-4 sm:flex-row">
				<div class="relative h-24 w-24">
					<Avatar.Root class="h-24 w-24">
						<Avatar.Fallback class="text-lg">
							{user?.username?.substring(0, 2).toUpperCase() ?? 'ME'}
						</Avatar.Fallback>
					</Avatar.Root>
				</div>
				<div class="flex flex-col gap-1 text-center sm:text-left">
					<h3 class="text-lg font-medium">{fullName}</h3>
					<p class="text-sm text-muted-foreground">{email}</p>
					<Button variant="outline" size="sm" class="mt-2 w-full sm:w-auto">
						{$t('profile.personalInfo.changeAvatar')}
					</Button>
				</div>
			</div>

			<div class="grid gap-4">
				<div class="grid gap-2">
					<Label for="username">{$t('profile.personalInfo.username')}</Label>
					<Input id="username" value={user?.username} disabled />
					<p class="text-xs text-muted-foreground">
						{$t('profile.personalInfo.usernameDescription')}
					</p>
				</div>

				<div class="grid gap-2">
					<Label for="fullName">{$t('profile.personalInfo.fullName')}</Label>
					<Input
						id="fullName"
						bind:value={fullName}
						placeholder={$t('profile.personalInfo.fullNamePlaceholder')}
					/>
				</div>

				<div class="grid gap-2">
					<Label for="email">{$t('profile.personalInfo.email')}</Label>
					<Input
						id="email"
						type="email"
						bind:value={email}
						placeholder={$t('profile.personalInfo.emailPlaceholder')}
					/>
				</div>

				<div class="grid gap-2">
					<Label for="bio">{$t('profile.personalInfo.bio')}</Label>
					<Textarea
						id="bio"
						bind:value={bio}
						placeholder={$t('profile.personalInfo.bioPlaceholder')}
					/>
				</div>

				<div class="flex justify-end">
					<Button type="submit" disabled={isLoading}>
						{isLoading ? $t('profile.personalInfo.saving') : $t('profile.personalInfo.saveChanges')}
					</Button>
				</div>
			</div>
		</form>
	</Card.Content>
</Card.Root>
