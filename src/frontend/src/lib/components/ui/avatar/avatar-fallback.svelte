<script lang="ts">
	import { Avatar as AvatarPrimitive } from 'bits-ui';
	import { cn } from '$lib/utils';

	let {
		ref = $bindable(null),
		class: className,
		children,
		...restProps
	}: AvatarPrimitive.FallbackProps = $props();
</script>

<AvatarPrimitive.Fallback
	bind:ref
	class={cn('flex h-full w-full items-center justify-center rounded-full bg-muted', className)}
	{...restProps}
>
	{@render children?.()}
</AvatarPrimitive.Fallback>
