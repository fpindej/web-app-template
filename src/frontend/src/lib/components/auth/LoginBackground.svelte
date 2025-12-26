<script lang="ts">
	import { spring } from 'svelte/motion';

	let { children } = $props();

	let mouseX = spring(0, { stiffness: 0.08, damping: 0.4 });
	let mouseY = spring(0, { stiffness: 0.08, damping: 0.4 });

	function handleMouseMove(event: MouseEvent) {
		mouseX.set(event.clientX);
		mouseY.set(event.clientY);
	}
</script>

<div
	class="relative min-h-screen overflow-hidden bg-background"
	onmousemove={handleMouseMove}
	role="presentation"
>
	<!-- Animated Background -->
	<div class="absolute inset-0 h-full w-full overflow-hidden">
		<div
			class="absolute top-0 right-0 -mt-20 -mr-20 h-[500px] w-[500px] rounded-full bg-primary/5 blur-3xl"
			style="transform: translate({-$mouseX * 0.12}px, {$mouseY * 0.12}px)"
		></div>
		<div
			class="absolute bottom-0 left-0 -mb-20 -ml-20 h-[500px] w-[500px] rounded-full bg-secondary/20 blur-3xl"
			style="transform: translate({$mouseX * 0.12}px, {-$mouseY * 0.12}px)"
		></div>
		<div
			class="absolute top-1/2 left-1/2 h-[500px] w-[500px] -translate-x-1/2 -translate-y-1/2"
			style="transform: translate(calc(-50% + {$mouseX * 0.08}px), calc(-50% + {$mouseY * 0.08}px))"
		>
			<div class="animate-blob h-full w-full rounded-full bg-primary/5 blur-3xl"></div>
		</div>
	</div>

	<div
		class="relative z-10 flex min-h-screen flex-col justify-center px-4 py-8 sm:px-6 sm:py-12 lg:px-8"
	>
		{@render children()}
	</div>
</div>

<style>
	@keyframes blob {
		0% {
			transform: scale(1);
		}
		33% {
			transform: scale(1.1);
		}
		66% {
			transform: scale(0.9);
		}
		100% {
			transform: scale(1);
		}
	}
	.animate-blob {
		animation: blob 7s infinite;
	}
</style>
