<script lang="ts">
	import { Button } from '$lib/components/ui/button';
	import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
	import { locale } from 'svelte-i18n';
	import { setLanguage } from '$lib/i18n';
	import { Check } from 'lucide-svelte';

	const languages = [
		{ code: 'en', label: 'English', flag: 'gb' },
		{ code: 'cs', label: 'Čeština', flag: 'cz' }
	];
</script>

<DropdownMenu.Root>
	<DropdownMenu.Trigger>
		{#snippet child({ props })}
			<Button variant="ghost" size="icon" {...props} aria-label="Select Language">
				<span class={`fi fi-${languages.find((l) => l.code === $locale)?.flag ?? 'gb'} text-lg`}
				></span>
			</Button>
		{/snippet}
	</DropdownMenu.Trigger>
	<DropdownMenu.Content align="end">
		{#each languages as lang (lang.code)}
			<DropdownMenu.Item onclick={() => setLanguage(lang.code)}>
				<span class={`fi fi-${lang.flag} me-2`}></span>
				<span>{lang.label}</span>
				{#if $locale === lang.code}
					<Check class="ms-auto h-4 w-4" />
				{/if}
			</DropdownMenu.Item>
		{/each}
	</DropdownMenu.Content>
</DropdownMenu.Root>
