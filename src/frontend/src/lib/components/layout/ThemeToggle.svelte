<script lang="ts">
	import { Sun, Moon } from 'lucide-svelte';
	import { Button } from '$lib/components/ui/button';
	import { toggleTheme, getTheme } from '$lib/state/theme.svelte';
	import * as m from '$lib/paraglide/messages';

	const themeLabels = {
		light: m.theme_light,
		dark: m.theme_dark,
		system: m.theme_system
	};
</script>

<Button
	variant="ghost"
	size="icon"
	onclick={toggleTheme}
	aria-label={`${m.common_theme()} (${themeLabels[getTheme()]()})`}
>
	<div class="relative h-5 w-5">
		<Sun class="absolute h-5 w-5 scale-100 rotate-0 transition-all dark:scale-0 dark:-rotate-90" />
		<Moon class="absolute h-5 w-5 scale-0 rotate-90 transition-all dark:scale-100 dark:rotate-0" />
	</div>
</Button>
