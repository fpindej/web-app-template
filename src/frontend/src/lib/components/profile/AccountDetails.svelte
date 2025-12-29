<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Badge } from '$lib/components/ui/badge';
	import { User as UserIcon, Shield } from '@lucide/svelte';
	import InfoItem from './InfoItem.svelte';
	import type { User } from '$lib/types';
	import * as m from '$lib/paraglide/messages';

	let { user }: { user: User | null | undefined } = $props();
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>{m.profile_account_title()}</Card.Title>
		<Card.Description>{m.profile_account_description()}</Card.Description>
	</Card.Header>
	<Card.Content class="space-y-6">
		<InfoItem icon={UserIcon} label={m.profile_account_userId()}>
			{user?.id}
		</InfoItem>

		<InfoItem icon={Shield} label={m.profile_account_roles()}>
			<div class="mt-1 flex flex-wrap gap-2">
				{#each user?.roles || [] as role (role)}
					<Badge variant="secondary">{role}</Badge>
				{:else}
					<span>{m.profile_account_noRoles()}</span>
				{/each}
			</div>
		</InfoItem>
	</Card.Content>
</Card.Root>
