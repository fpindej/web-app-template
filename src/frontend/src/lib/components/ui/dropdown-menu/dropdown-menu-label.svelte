<script lang="ts">
	import { cn } from '$lib/utils';
	import type { HTMLAttributes } from 'svelte/elements';

	let {
		ref = $bindable(null),
		class: className,
		inset,
		children,
		...restProps
	}: HTMLAttributes<HTMLDivElement> & {
		ref?: HTMLDivElement | null;
		inset?: boolean;
	} = $props();
</script>

<div
	bind:this={ref}
	data-slot="dropdown-menu-label"
	data-inset={inset}
	class={cn('px-2 py-1.5 text-sm font-semibold data-[inset]:ps-8', className)}
	{...restProps}
>
	{@render children?.()}
</div>
