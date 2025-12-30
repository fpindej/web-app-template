<script lang="ts">
	import { cn } from '$lib/utils';
	import { SidebarNav, ThemeToggle, LanguageSelector, UserNav } from '$lib/components/layout';
	import { Package2, PanelLeftClose, PanelLeft } from '@lucide/svelte';
	import { resolve } from '$app/paths';
	import type { User } from '$lib/types';
	import * as m from '$lib/paraglide/messages';
	import { isCollapsed, toggleSidebar } from '$lib/state';
	import * as Tooltip from '$lib/components/ui/tooltip';
	import { Button } from '$lib/components/ui/button';

	let { class: className, user }: { class?: string; user: User | null | undefined } = $props();
</script>

<div
	class={cn('flex h-full flex-col gap-2 transition-all duration-300', className)}
	data-collapsed={isCollapsed()}
>
	<div class="flex-1 overflow-auto py-4">
		<div class="px-3 py-2">
			<div class={cn('mb-2 flex items-center', isCollapsed() ? 'justify-center px-0' : 'px-4')}>
				<a
					href={resolve('/')}
					class={cn(
						'flex items-center gap-2 font-semibold',
						isCollapsed() ? 'text-base' : 'text-lg'
					)}
				>
					<Package2 class="h-6 w-6 shrink-0" />
					{#if !isCollapsed()}
						<span>{m.app_name()}</span>
					{/if}
				</a>
			</div>
			<div class="space-y-1">
				<SidebarNav collapsed={isCollapsed()} />
			</div>
		</div>
	</div>
	<div class="border-t p-4">
		<div class={cn('flex items-center', isCollapsed() ? 'flex-col gap-2' : 'justify-between')}>
			<div class={cn('flex items-center', isCollapsed() ? 'flex-col gap-1' : 'gap-1')}>
				<LanguageSelector collapsed={isCollapsed()} />
				<ThemeToggle collapsed={isCollapsed()} />
			</div>
			{#if user}
				<UserNav {user} collapsed={isCollapsed()} />
			{/if}
		</div>
		<!-- Collapse toggle button -->
		<div class={cn('mt-3 flex', isCollapsed() ? 'justify-center' : 'justify-end')}>
			<Tooltip.Root>
				<Tooltip.Trigger>
					{#snippet child({ props })}
						<Button
							variant="ghost"
							size="icon"
							onclick={toggleSidebar}
							class="h-8 w-8"
							aria-label={isCollapsed() ? m.nav_expand() : m.nav_collapse()}
							{...props}
						>
							{#if isCollapsed()}
								<PanelLeft class="h-4 w-4" />
							{:else}
								<PanelLeftClose class="h-4 w-4" />
							{/if}
						</Button>
					{/snippet}
				</Tooltip.Trigger>
				<Tooltip.Content side="right">
					{isCollapsed() ? m.nav_expand() : m.nav_collapse()}
				</Tooltip.Content>
			</Tooltip.Root>
		</div>
	</div>
</div>
