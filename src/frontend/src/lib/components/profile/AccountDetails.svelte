<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Badge } from '$lib/components/ui/badge';
	import { User, Shield } from 'lucide-svelte';
	import InfoItem from './InfoItem.svelte';
	import type { components } from '$lib/api/v1';
	import * as m from '$lib/paraglide/messages';

	type UserType = components['schemas']['MeResponse'];

	let { user }: { user: UserType | null | undefined } = $props();
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>{m.profile_accountDetails_title()}</Card.Title>
		<Card.Description>{m.profile_accountDetails_description()}</Card.Description>
	</Card.Header>
	<Card.Content class="space-y-6">
		<InfoItem icon={User} label={m.profile_accountDetails_userId()}>
			{user?.id}
		</InfoItem>

		<InfoItem icon={Shield} label={m.profile_accountDetails_roles()}>
			<div class="mt-1 flex flex-wrap gap-2">
				{#each user?.roles || [] as role (role)}
					<Badge variant="secondary">{role}</Badge>
				{:else}
					<span>{m.profile_accountDetails_noRoles()}</span>
				{/each}
			</div>
		</InfoItem>
	</Card.Content>
</Card.Root>
