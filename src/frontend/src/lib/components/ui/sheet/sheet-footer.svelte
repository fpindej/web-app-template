<script lang="ts">
	import { cn } from '$lib/utils';

	let {
		class: className,
		children,
		...restProps
	}: import('svelte/elements').HTMLAttributes<HTMLDivElement> = $props();
</script>

<div
	class={cn('flex flex-col-reverse sm:flex-row sm:justify-end sm:space-x-2', className)}
	{...restProps}
>
	{@render children?.()}
</div>
