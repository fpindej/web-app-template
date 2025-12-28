<script lang="ts">
	import './layout.css';
	import 'flag-icons/css/flag-icons.min.css';
	import favicon from '$lib/assets/favicon.svg';
	import { onMount } from 'svelte';
	import { initTheme } from '$lib/theme.svelte';
	import * as m from '$lib/paraglide/messages';
	import { Toaster } from '$lib/components/ui/sonner';
	import {
		globalShortcuts,
		shortcutsState,
		getAllShortcuts,
		getShortcutSymbol
	} from '$lib/stores/shortcuts.svelte';
	import { goto } from '$app/navigation';
	import { base } from '$app/paths';
	import { logout } from '$lib/services/auth';
	import * as Dialog from '$lib/components/ui/dialog';

	let { children } = $props();

	onMount(() => {
		return initTheme();
	});

	async function handleSettings() {
		// eslint-disable-next-line svelte/no-navigation-without-resolve
		await goto(`${base}/settings`);
	}
</script>

<svelte:window
	use:globalShortcuts={{
		settings: handleSettings,
		logout: logout
	}}
/>

<Dialog.Root bind:open={shortcutsState.isHelpOpen}>
	<Dialog.Content>
		<Dialog.Header>
			<Dialog.Title>{m.common_shortcuts()}</Dialog.Title>
		</Dialog.Header>
		<div class="grid gap-4 py-4">
			{#each getAllShortcuts() as shortcut (shortcut.action)}
				<div class="flex items-center justify-between">
					<span class="text-sm text-muted-foreground">{shortcut.description()}</span>
					<kbd
						class="pointer-events-none inline-flex h-5 items-center gap-1 rounded border bg-muted px-1.5 font-mono text-[10px] font-medium text-muted-foreground opacity-100 select-none"
					>
						{getShortcutSymbol(shortcut.action)}
					</kbd>
				</div>
			{/each}
		</div>
	</Dialog.Content>
</Dialog.Root>

<svelte:head>
	<title>{m.common_appName()}</title>
	<meta name="description" content={m.common_meta_description()} />
	<link rel="icon" href={favicon} />
</svelte:head>

<Toaster />
{@render children()}
