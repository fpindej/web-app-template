<script lang="ts">
	import { ProfileForm, AccountDetails } from '$lib/components/profile';
	import type { PageData } from './$types';
	import * as m from '$lib/paraglide/messages';

	let { data }: { data: PageData } = $props();
</script>

<svelte:head>
	<title>{m.common_meta_titleTemplate({ title: m.common_meta_profile_title() })}</title>
	<meta name="description" content={m.common_meta_profile_description()} />
</svelte:head>

<div class="space-y-6">
	<div>
		<h3 class="text-lg font-medium">{m.profile_title()}</h3>
		<p class="text-sm text-muted-foreground">{m.profile_description()}</p>
	</div>
	<div class="h-px w-full bg-border"></div>
	<div class="grid gap-6 md:grid-cols-2">
		<ProfileForm user={data.user} />
		<AccountDetails user={data.user} />
	</div>
</div>
