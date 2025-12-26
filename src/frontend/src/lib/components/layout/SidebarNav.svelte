<script lang="ts">
	import { page } from '$app/stores';
	import { base } from '$app/paths';
	import { cn } from '$lib/utils';
	import { buttonVariants } from '$lib/components/ui/button';
	import { LayoutDashboard, Settings, User } from 'lucide-svelte';
	import { t } from '$lib/i18n';
	import type { TranslationKey } from '$lib/types/i18n';
	import type { ComponentType } from 'svelte';

	let items: { title: TranslationKey; href: string; icon: ComponentType }[] = [
		{
			title: 'common.dashboard',
			href: `${base}/`,
			icon: LayoutDashboard
		},
		{
			title: 'common.profile',
			href: `${base}/profile`,
			icon: User
		},
		{
			title: 'common.settings',
			href: `${base}/settings`,
			icon: Settings
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
			{$t(item.title) || item.title}
		</a>
		<!-- eslint-enable svelte/no-navigation-without-resolve -->
	{/each}
</nav>
