<script lang="ts">
	import { Sun, Moon } from 'lucide-svelte';
	import { Button } from '$lib/components/ui/button';
	import { toggleTheme, getTheme } from '$lib/theme.svelte';
	import { t } from '$lib/i18n';
</script>

<Button
	variant="ghost"
	size="icon"
	onclick={toggleTheme}
	aria-label={`${$t('common.theme')} (current: ${getTheme()})`}
>
	<div class="relative h-[1.2rem] w-[1.2rem]">
		<Sun
			class="absolute h-[1.2rem] w-[1.2rem] scale-100 rotate-0 transition-all dark:scale-0 dark:-rotate-90"
		/>
		<Moon
			class="absolute h-[1.2rem] w-[1.2rem] scale-0 rotate-90 transition-all dark:scale-100 dark:rotate-0"
		/>
	</div>
</Button>
