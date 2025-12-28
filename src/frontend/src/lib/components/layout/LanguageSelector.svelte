<script lang="ts">
	import { Button } from '$lib/components/ui/button';
	import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
	import { getLocale, setLocale, locales } from '$lib/paraglide/runtime';
	import { Check } from 'lucide-svelte';

	type AvailableLanguageTag = (typeof locales)[number];

	const languageMetadata: Record<AvailableLanguageTag, { label: string; flag: string }> = {
		en: { label: 'English', flag: 'gb' },
		cs: { label: 'Čeština', flag: 'cz' }
	};

	const languages = locales.map((code) => ({
		code,
		...languageMetadata[code]
	}));
</script>

<DropdownMenu.Root>
	<DropdownMenu.Trigger>
		{#snippet child({ props })}
			<Button variant="ghost" size="icon" {...props} aria-label="Select Language">
				<span class={`fi fi-${languages.find((l) => l.code === getLocale())?.flag ?? 'gb'} text-lg`}
				></span>
			</Button>
		{/snippet}
	</DropdownMenu.Trigger>
	<DropdownMenu.Content align="end">
		{#each languages as lang (lang.code)}
			<DropdownMenu.Item
				onclick={() => {
					setLocale(lang.code);
					document.cookie = `PARAGLIDE_LOCALE=${lang.code}; path=/; max-age=31536000; SameSite=Lax`;
				}}
			>
				<span class={`fi fi-${lang.flag} me-2`}></span>
				<span>{lang.label}</span>
				{#if getLocale() === lang.code}
					<Check class="ms-auto h-4 w-4" />
				{/if}
			</DropdownMenu.Item>
		{/each}
	</DropdownMenu.Content>
</DropdownMenu.Root>
