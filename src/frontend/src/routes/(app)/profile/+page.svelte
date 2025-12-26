<script lang="ts">
	import { ProfileForm, AccountDetails } from '$lib/components/profile';
	import type { PageData } from './$types';
	import { t } from '$lib/i18n';

	let { data }: { data: PageData } = $props();
</script>

<div class="space-y-6">
	<div>
		<h3 class="text-lg font-medium">{$t('profile.title')}</h3>
		<p class="text-sm text-muted-foreground">{$t('profile.description')}</p>
	</div>
	<div class="h-px w-full bg-border"></div>
	<div class="grid gap-6 md:grid-cols-2">
		<ProfileForm user={data.user} />
		<AccountDetails user={data.user} />
	</div>
</div>
