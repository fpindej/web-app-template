<script lang="ts">
	import * as Avatar from '$lib/components/ui/avatar';
	import { Button } from '$lib/components/ui/button';
	import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
	import * as Tooltip from '$lib/components/ui/tooltip';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import * as m from '$lib/paraglide/messages';
	import type { User } from '$lib/types';
	import { getShortcutSymbol, ShortcutAction } from '$lib/state/shortcuts.svelte';
	import { logout } from '$lib/auth';

	interface Props {
		user: User | null | undefined;
		collapsed?: boolean;
	}

	let { user, collapsed = false }: Props = $props();

	// Track dropdown state to hide tooltip when open
	let dropdownOpen = $state(false);

	function getInitials(name: string) {
		return name
			.split(' ')
			.map((n) => n[0])
			.join('')
			.toUpperCase()
			.slice(0, 2);
	}
</script>

<DropdownMenu.Root bind:open={dropdownOpen}>
	<Tooltip.Root>
		<Tooltip.Trigger>
			{#snippet child({ props: tooltipProps })}
				<DropdownMenu.Trigger>
					{#snippet child({ props })}
						<Button variant="ghost" size="icon" class="rounded-full" {...props} {...tooltipProps}>
							<Avatar.Root class="h-7 w-7">
								{#if user?.avatarUrl}
									<Avatar.Image src={user.avatarUrl} alt={user.username || m.common_user()} />
								{/if}
								<Avatar.Fallback>{getInitials(user?.username || m.common_user())}</Avatar.Fallback>
							</Avatar.Root>
						</Button>
					{/snippet}
				</DropdownMenu.Trigger>
			{/snippet}
		</Tooltip.Trigger>
		{#if !dropdownOpen}
			<Tooltip.Content side={collapsed ? 'right' : 'top'}>
				{user?.username || m.common_user()}
			</Tooltip.Content>
		{/if}
	</Tooltip.Root>
	<DropdownMenu.Content class="w-56" align="end">
		<DropdownMenu.Label class="font-normal">
			<div class="flex flex-col space-y-1">
				<p class="text-sm leading-none font-medium">{user?.username}</p>
			</div>
		</DropdownMenu.Label>
		<DropdownMenu.Separator />
		<DropdownMenu.Group>
			<DropdownMenu.Item onclick={() => goto(resolve('/profile'))}>
				{m.nav_profile()}
			</DropdownMenu.Item>
			<DropdownMenu.Item onclick={() => goto(resolve('/settings'))}>
				{m.nav_settings()}
				<DropdownMenu.Shortcut>{getShortcutSymbol(ShortcutAction.Settings)}</DropdownMenu.Shortcut>
			</DropdownMenu.Item>
		</DropdownMenu.Group>
		<DropdownMenu.Separator />
		<DropdownMenu.Item onclick={logout}>
			{m.nav_logout()}
			<DropdownMenu.Shortcut>{getShortcutSymbol(ShortcutAction.Logout)}</DropdownMenu.Shortcut>
		</DropdownMenu.Item>
	</DropdownMenu.Content>
</DropdownMenu.Root>
