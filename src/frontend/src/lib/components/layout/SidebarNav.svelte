<script lang="ts">
	import { page } from '$app/state';
	import { resolve } from '$app/paths';
	import { cn } from '$lib/utils';
	import { buttonVariants } from '$lib/components/ui/button';
	import {
		LayoutDashboard,
		ChartPie,
		FileText,
		Users,
		Shield,
		type IconProps
	} from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';
	import * as Tooltip from '$lib/components/ui/tooltip';
	import type { Component } from 'svelte';
	import type { User } from '$lib/types';

	interface Props {
		collapsed?: boolean;
		onNavigate?: () => void;
		user?: User | null;
	}

	let { collapsed = false, onNavigate, user }: Props = $props();

	type NavItem = { title: () => string; href: string; icon: Component<IconProps> };

	let items: NavItem[] = [
		{
			title: m.nav_dashboard,
			href: resolve('/'),
			icon: LayoutDashboard
		},
		{
			title: m.nav_analytics,
			href: resolve('/analytics'),
			icon: ChartPie
		},
		{
			title: m.nav_reports,
			href: resolve('/reports'),
			icon: FileText
		}
	];

	let adminItems: NavItem[] = [
		{
			title: m.nav_adminUsers,
			href: resolve('/admin/users'),
			icon: Users
		},
		{
			title: m.nav_adminRoles,
			href: resolve('/admin/roles'),
			icon: Shield
		}
	];

	let isAdmin = $derived(user?.roles?.some((r) => r === 'Admin' || r === 'SuperAdmin') ?? false);

	function isActive(href: string, pathname: string) {
		if (href === resolve('/')) {
			return pathname === href;
		}
		return pathname.startsWith(href);
	}
</script>

<!-- eslint-disable svelte/no-navigation-without-resolve -- hrefs are pre-resolved using resolve() in items array -->
{#snippet navItem(item: NavItem)}
	{@const active = isActive(item.href, page.url.pathname)}
	{#if collapsed}
		<Tooltip.Root>
			<Tooltip.Trigger>
				{#snippet child({ props })}
					<a
						href={item.href}
						class={cn(
							buttonVariants({
								variant: active ? 'default' : 'ghost',
								size: 'icon'
							}),
							'h-9 w-9',
							active && 'dark:bg-muted dark:text-white dark:hover:bg-muted dark:hover:text-white'
						)}
						aria-current={active ? 'page' : undefined}
						aria-label={item.title()}
						onclick={onNavigate}
						{...props}
					>
						<item.icon class="h-4 w-4" />
					</a>
				{/snippet}
			</Tooltip.Trigger>
			<Tooltip.Content side="right">
				{item.title()}
			</Tooltip.Content>
		</Tooltip.Root>
	{:else}
		<a
			href={item.href}
			class={cn(
				buttonVariants({
					variant: active ? 'default' : 'ghost',
					size: 'sm'
				}),
				active && 'dark:bg-muted dark:text-white dark:hover:bg-muted dark:hover:text-white',
				'justify-start'
			)}
			aria-current={active ? 'page' : undefined}
			onclick={onNavigate}
		>
			<item.icon class="me-2 h-4 w-4" />
			{item.title()}
		</a>
	{/if}
{/snippet}

<nav class={cn('grid gap-1', collapsed ? 'justify-center px-2' : 'px-2')}>
	{#each items as item (item.href)}
		{@render navItem(item)}
	{/each}

	{#if isAdmin}
		<div class="my-2 h-px w-full bg-border"></div>
		{#if !collapsed}
			<span class="mb-1 px-3 text-xs font-semibold tracking-wider text-muted-foreground uppercase">
				{m.nav_admin()}
			</span>
		{/if}
		{#each adminItems as item (item.href)}
			{@render navItem(item)}
		{/each}
	{/if}
</nav>
