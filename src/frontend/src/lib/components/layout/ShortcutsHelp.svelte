<script lang="ts">
	import * as Dialog from '$lib/components/ui/dialog';
	import * as m from '$lib/paraglide/messages';
	import { shortcutsState, getAllShortcuts, getShortcutSymbol } from '$lib/state/shortcuts.svelte';
</script>

<Dialog.Root bind:open={shortcutsState.isHelpOpen}>
	<Dialog.Content>
		<Dialog.Header>
			<Dialog.Title>{m.shortcuts_title()}</Dialog.Title>
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
