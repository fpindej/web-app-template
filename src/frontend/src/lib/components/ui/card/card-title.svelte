<script lang="ts">
	import type { WithElementRef } from 'bits-ui';
	import type { HTMLAttributes } from 'svelte/elements';
	import { cn } from '$lib/utils';

	let {
		class: className,
		ref = $bindable(null),
		children,
		...restProps
	}: WithElementRef<HTMLAttributes<HTMLHeadingElement>> = $props();
</script>

<h3
	bind:this={ref}
	class={cn('leading-none font-semibold tracking-tight', className)}
	{...restProps}
>
	{@render children?.()}
</h3>
