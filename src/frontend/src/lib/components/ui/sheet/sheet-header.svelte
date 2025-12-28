<script lang="ts">
	import { cn } from '$lib/utils';

	let {
		class: className,
		children,
		...restProps
	}: import('svelte/elements').HTMLAttributes<HTMLDivElement> = $props();
</script>

<div class={cn('flex flex-col space-y-2 text-center sm:text-left', className)} {...restProps}>
	{@render children?.()}
</div>
