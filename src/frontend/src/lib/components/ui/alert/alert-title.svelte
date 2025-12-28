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

<h5
	bind:this={ref}
	class={cn('mb-1 leading-none font-medium tracking-tight', className)}
	{...restProps}
>
	{@render children?.()}
</h5>
