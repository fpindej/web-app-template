<script lang="ts">
	import { resolve } from '$app/paths';
	import { buttonVariants } from '$lib/components/ui/button';
	import { UserDetailCards } from '$lib/components/admin';
	import { ArrowLeft } from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';
	import { cn } from '$lib/utils';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();
</script>

<!-- eslint-disable svelte/no-navigation-without-resolve -- hrefs are pre-resolved using resolve() -->
<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_adminUserDetail_title() })}</title>
	<meta name="description" content={m.meta_adminUserDetail_description()} />
</svelte:head>

<div class="space-y-6">
	<div class="flex items-center gap-4">
		<a
			href={resolve('/admin/users')}
			class={cn(buttonVariants({ variant: 'ghost', size: 'icon' }), 'h-8 w-8')}
			aria-label={m.admin_userDetail_backToUsers()}
		>
			<ArrowLeft class="h-4 w-4" />
		</a>
		<div>
			<h3 class="text-lg font-medium">
				{#if data.adminUser?.firstName || data.adminUser?.lastName}
					{[data.adminUser?.firstName, data.adminUser?.lastName].filter(Boolean).join(' ')}
				{:else}
					{data.adminUser?.username}
				{/if}
			</h3>
			<p class="text-sm text-muted-foreground">{data.adminUser?.email}</p>
		</div>
	</div>
	<div class="h-px w-full bg-border"></div>

	{#if data.adminUser && data.user}
		<UserDetailCards user={data.adminUser} roles={data.roles ?? []} currentUser={data.user} />
	{/if}
</div>
