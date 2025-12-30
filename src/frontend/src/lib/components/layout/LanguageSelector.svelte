<script lang="ts">
	import { Button } from '$lib/components/ui/button';
	import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
	import * as Tooltip from '$lib/components/ui/tooltip';
	import { getLocale, setLocale, locales, baseLocale } from '$lib/paraglide/runtime';
	import { Check } from '@lucide/svelte';
	import * as m from '$lib/paraglide/messages';
	import { LANGUAGE_METADATA } from '$lib/config';

	interface Props {
		collapsed?: boolean;
	}

	let { collapsed = false }: Props = $props();

	type AvailableLanguageTag = (typeof locales)[number];

	const languages = locales.map((code) => ({
		code,
		...LANGUAGE_METADATA[code]
	}));

	// Track dropdown state to hide tooltip when open
	let dropdownOpen = $state(false);
</script>

<DropdownMenu.Root bind:open={dropdownOpen}>
	<Tooltip.Root>
		<Tooltip.Trigger>
			{#snippet child({ props: tooltipProps })}
				<DropdownMenu.Trigger>
					{#snippet child({ props })}
						<Button
							variant="ghost"
							size="icon"
							{...props}
							{...tooltipProps}
							aria-label={m.common_language()}
						>
							<span
								class={`fi fi-${languages.find((l) => l.code === getLocale())?.flag ?? LANGUAGE_METADATA[baseLocale as AvailableLanguageTag].flag} rounded-sm`}
							></span>
						</Button>
					{/snippet}
				</DropdownMenu.Trigger>
			{/snippet}
		</Tooltip.Trigger>
		{#if !dropdownOpen}
			<Tooltip.Content side={collapsed ? 'right' : 'top'}>
				{m.common_language()}
			</Tooltip.Content>
		{/if}
	</Tooltip.Root>
	<DropdownMenu.Content align="end">
		{#each languages as lang (lang.code)}
			<DropdownMenu.Item
				onclick={() => {
					setLocale(lang.code);
				}}
			>
				<span class={`fi fi-${lang.flag} me-2 h-3 w-4 rounded-sm`}></span>
				<span>{lang.label}</span>
				{#if getLocale() === lang.code}
					<Check class="ms-auto h-4 w-4" />
				{/if}
			</DropdownMenu.Item>
		{/each}
	</DropdownMenu.Content>
</DropdownMenu.Root>
