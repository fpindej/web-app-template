<script lang="ts">
	import { Dialog as SheetPrimitive } from 'bits-ui';
	import { cn } from '$lib/utils';

	let {
		ref = $bindable(null),
		class: className,
		children,
		...restProps
	}: SheetPrimitive.TriggerProps = $props();
</script>

<SheetPrimitive.Trigger bind:ref class={cn(className)} {...restProps}>
	{@render children?.()}
</SheetPrimitive.Trigger>
