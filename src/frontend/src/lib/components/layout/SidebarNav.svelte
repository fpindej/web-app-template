<script lang="ts">
	import { page } from '$app/stores';
	import { base } from '$app/paths';
	import { cn } from '$lib/utils';
	import { buttonVariants } from '$lib/components/ui/button';
	import { LayoutDashboard, ChartPie, FileText, type IconProps } from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';
	import type { Component } from 'svelte';

	let items: { title: () => string; href: string; icon: Component<IconProps> }[] = [
		{
			title: m.nav_dashboard,
			href: `${base}/`,
			icon: LayoutDashboard
		},
		{
			title: m.nav_analytics,
			href: `${base}/analytics`,
			icon: ChartPie
		},
		{
			title: m.nav_reports,
			href: `${base}/reports`,
			icon: FileText
		}
	];

	function isActive(href: string, pathname: string) {
		if (href === `${base}/`) {
			return pathname === href;
		}
		return pathname.startsWith(href);
	}
</script>

<nav
	class="grid gap-1 px-2 group-[[data-collapsed=true]]:justify-center group-[[data-collapsed=true]]:px-2"
>
	{#each items as item (item.href)}
		{@const active = isActive(item.href, $page.url.pathname)}
		<!-- eslint-disable svelte/no-navigation-without-resolve -->
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
		>
			<item.icon class="me-2 h-4 w-4" />
			{item.title()}
		</a>
		<!-- eslint-enable svelte/no-navigation-without-resolve -->
	{/each}
</nav>
