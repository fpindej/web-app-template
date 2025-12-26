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
	<div class="relative h-5 w-5">
		<Sun class="absolute h-5 w-5 scale-100 rotate-0 transition-all dark:scale-0 dark:-rotate-90" />
		<Moon class="absolute h-5 w-5 scale-0 rotate-90 transition-all dark:scale-100 dark:rotate-0" />
	</div>
</Button>
