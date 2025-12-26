<script lang="ts">
	import * as Avatar from '$lib/components/ui/avatar';
	import { Button } from '$lib/components/ui/button';
	import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
	import { browserClient } from '$lib/api/client';
	import { goto, invalidateAll } from '$app/navigation';
	import { base } from '$app/paths';
	import { t } from '$lib/i18n';
	import type { components } from '$lib/api/v1';

	type User = components['schemas']['MeResponse'];

	let { user }: { user: User | null | undefined } = $props();

	function getInitials(name: string) {
		return name
			.split(' ')
			.map((n) => n[0])
			.join('')
			.toUpperCase()
			.slice(0, 2);
	}

	async function logout() {
		await browserClient.POST('/api/auth/logout');
		await invalidateAll();
		// eslint-disable-next-line svelte/no-navigation-without-resolve
		await goto(`${base}/login`);
	}
</script>

<!-- eslint-disable svelte/no-navigation-without-resolve -->
<DropdownMenu.Root>
	<DropdownMenu.Trigger>
		{#snippet child({ props })}
			<Button variant="ghost" class="relative h-8 w-8 rounded-full" {...props}>
				<Avatar.Root class="h-8 w-8">
					<Avatar.Fallback>{getInitials(user?.username || $t('common.user'))}</Avatar.Fallback>
				</Avatar.Root>
			</Button>
		{/snippet}
	</DropdownMenu.Trigger>
	<DropdownMenu.Content class="w-56" align="end">
		<DropdownMenu.Label class="font-normal">
			<div class="flex flex-col space-y-1">
				<p class="text-sm leading-none font-medium">{user?.username}</p>
			</div>
		</DropdownMenu.Label>
		<DropdownMenu.Separator />
		<DropdownMenu.Group>
			<DropdownMenu.Item onclick={() => goto(`${base}/profile`)}>
				{$t('common.profile')}
			</DropdownMenu.Item>
			<DropdownMenu.Item onclick={() => goto(`${base}/settings`)}>
				{$t('common.settings')}
			</DropdownMenu.Item>
		</DropdownMenu.Group>
		<DropdownMenu.Separator />
		<DropdownMenu.Item onclick={logout}>
			{$t('navbar.logout')}
		</DropdownMenu.Item>
	</DropdownMenu.Content>
</DropdownMenu.Root>
