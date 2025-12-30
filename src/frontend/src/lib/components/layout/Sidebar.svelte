<script lang="ts">
	import { cn } from '$lib/utils';
	import { SidebarNav, ThemeToggle, LanguageSelector, UserNav } from '$lib/components/layout';
	import { Package2, PanelLeftClose, PanelLeft } from '@lucide/svelte';
	import { resolve } from '$app/paths';
	import type { User } from '$lib/types';
	import * as m from '$lib/paraglide/messages';
	import { sidebarState, toggleSidebar } from '$lib/state';
	import * as Tooltip from '$lib/components/ui/tooltip';
	import { Button } from '$lib/components/ui/button';

	interface Props {
		class?: string;
		user: User | null | undefined;
	}

	let { class: className, user }: Props = $props();

	let collapsed = $derived(sidebarState.collapsed);
</script>

<div
	class={cn('flex h-full flex-col gap-2 transition-all duration-300', className)}
	data-collapsed={collapsed}
>
	<div class="flex-1 overflow-auto py-4">
		<div class="px-3 py-2">
			<div class={cn('mb-4 flex items-center', collapsed ? 'justify-center px-0' : 'px-2')}>
				<a
					href={resolve('/')}
					class={cn('flex items-center gap-2 font-semibold', collapsed ? 'text-base' : 'text-lg')}
				>
					<Package2 class="h-6 w-6 shrink-0" />
					{#if !collapsed}
						<span>{m.app_name()}</span>
					{/if}
				</a>
			</div>
			<div class="space-y-1">
				<SidebarNav {collapsed} />
			</div>
		</div>
	</div>
	<div class="border-t p-3">
		<div class={cn('flex items-center gap-1', collapsed ? 'flex-col' : 'flex-row justify-between')}>
			{#if collapsed}
				<!-- Collapsed: stack vertically -->
				<LanguageSelector />
				<ThemeToggle collapsed />
				{#if user}
					<UserNav {user} />
				{/if}
				<Tooltip.Root>
					<Tooltip.Trigger>
						{#snippet child({ props })}
							<Button
								variant="ghost"
								size="icon"
								class="mt-2 h-9 w-9"
								aria-label={m.nav_expand()}
								{...props}
								onclick={toggleSidebar}
							>
								<PanelLeft class="h-4 w-4" />
							</Button>
						{/snippet}
					</Tooltip.Trigger>
					<Tooltip.Content side="right">
						{m.nav_expand()}
					</Tooltip.Content>
				</Tooltip.Root>
			{:else}
				<!-- Expanded: row layout -->
				<div class="flex items-center gap-1">
					<LanguageSelector />
					<ThemeToggle />
				</div>
				<div class="flex items-center gap-1">
					{#if user}
						<UserNav {user} />
					{/if}
					<Tooltip.Root>
						<Tooltip.Trigger>
							{#snippet child({ props })}
								<Button
									variant="ghost"
									size="icon"
									class="h-9 w-9"
									aria-label={m.nav_collapse()}
									{...props}
									onclick={toggleSidebar}
								>
									<PanelLeftClose class="h-4 w-4" />
								</Button>
							{/snippet}
						</Tooltip.Trigger>
						<Tooltip.Content side="right">
							{m.nav_collapse()}
						</Tooltip.Content>
					</Tooltip.Root>
				</div>
			{/if}
		</div>
	</div>
</div>
