<script lang="ts">
	import type { WithElementRef } from 'bits-ui';
	import type { HTMLInputAttributes } from 'svelte/elements';
	import { cn } from '$lib/utils';

	let {
		class: className,
		value = $bindable(),
		ref = $bindable(null),
		type = 'text',
		...restProps
	}: WithElementRef<HTMLInputAttributes> = $props();
</script>

<input
	bind:this={ref}
	bind:value
	{type}
	class={cn(
		'flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50',
		className
	)}
	{...restProps}
/>
