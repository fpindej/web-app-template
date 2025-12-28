<script lang="ts" module>
	import { type VariantProps, tv } from 'tailwind-variants';

	export const badgeVariants = tv({
		base: 'inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none',
		variants: {
			variant: {
				default: 'border-transparent bg-primary text-primary-foreground hover:bg-primary/80',
				secondary:
					'border-transparent bg-secondary text-secondary-foreground hover:bg-secondary/80',
				destructive:
					'border-transparent bg-destructive text-destructive-foreground hover:bg-destructive/80',
				success: 'border-transparent bg-success text-success-foreground hover:bg-success/80',
				warning: 'border-transparent bg-warning text-warning-foreground hover:bg-warning/80',
				outline: 'text-foreground'
			}
		},
		defaultVariants: {
			variant: 'default'
		}
	});
</script>

<script lang="ts">
	import type { WithElementRef } from 'bits-ui';
	import type { HTMLAttributes } from 'svelte/elements';
	import { cn } from '$lib/utils';

	type BadgeVariant = VariantProps<typeof badgeVariants>['variant'];

	let {
		class: className,
		variant = 'default',
		ref = $bindable(null),
		children,
		...restProps
	}: WithElementRef<HTMLAttributes<HTMLDivElement>> & {
		variant?: BadgeVariant;
	} = $props();
</script>

<div bind:this={ref} class={cn(badgeVariants({ variant }), className)} {...restProps}>
	{@render children?.()}
</div>
