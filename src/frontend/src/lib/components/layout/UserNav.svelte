<script lang="ts">
	import * as Avatar from '$lib/components/ui/avatar';
	import { Button } from '$lib/components/ui/button';
	import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
	import { goto } from '$app/navigation';
	import { base } from '$app/paths';
	import * as m from '$lib/paraglide/messages';
	import type { User } from '$lib/types';
	import { getShortcutSymbol, ShortcutAction } from '$lib/state/shortcuts.svelte';
	import { logout } from '$lib/services/auth';

	let { user }: { user: User | null | undefined } = $props();

	function getInitials(name: string) {
		return name
			.split(' ')
			.map((n) => n[0])
			.join('')
			.toUpperCase()
			.slice(0, 2);
	}
</script>

<!-- eslint-disable svelte/no-navigation-without-resolve -->
<DropdownMenu.Root>
	<DropdownMenu.Trigger>
		{#snippet child({ props })}
			<Button variant="ghost" class="relative h-8 w-8 rounded-full" {...props}>
				<Avatar.Root class="h-8 w-8">
					{#if user?.avatarUrl}
						<Avatar.Image src={user.avatarUrl} alt={user.username || m.common_user()} />
					{/if}
					<Avatar.Fallback>{getInitials(user?.username || m.common_user())}</Avatar.Fallback>
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
				{m.common_profile()}
			</DropdownMenu.Item>
			<DropdownMenu.Item onclick={() => goto(`${base}/settings`)}>
				{m.common_settings()}
				<DropdownMenu.Shortcut>{getShortcutSymbol(ShortcutAction.Settings)}</DropdownMenu.Shortcut>
			</DropdownMenu.Item>
		</DropdownMenu.Group>
		<DropdownMenu.Separator />
		<DropdownMenu.Item onclick={logout}>
			{m.navbar_logout()}
			<DropdownMenu.Shortcut>{getShortcutSymbol(ShortcutAction.Logout)}</DropdownMenu.Shortcut>
		</DropdownMenu.Item>
	</DropdownMenu.Content>
</DropdownMenu.Root>
