<script lang="ts">
	import * as Avatar from '$lib/components/ui/avatar';
	import { AvatarDialog } from '$lib/components/profile';
	import type { User } from '$lib/types';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		user: User | null | undefined;
	}

	let { user }: Props = $props();

	let avatarDialogOpen = $state(false);

	// Computed display name
	const displayName = $derived.by(() => {
		const first = user?.firstName;
		const last = user?.lastName;
		if (first || last) {
			return [first, last].filter(Boolean).join(' ');
		}
		return user?.username ?? m.common_user();
	});

	// Computed initials for avatar
	const initials = $derived.by(() => {
		const first = user?.firstName;
		const last = user?.lastName;
		if (first && last) {
			return `${first[0]}${last[0]}`.toUpperCase();
		}
		if (first) {
			return first.substring(0, 2).toUpperCase();
		}
		return user?.username?.substring(0, 2).toUpperCase() ?? 'ME';
	});
</script>

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
		<AvatarDialog
			bind:open={avatarDialogOpen}
			currentAvatarUrl={user?.avatarUrl}
			{displayName}
			{initials}
		/>
	</div>
</div>
