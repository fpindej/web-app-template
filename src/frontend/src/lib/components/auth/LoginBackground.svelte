<script lang="ts">
	import type { Snippet } from 'svelte';

	interface Props {
		children: Snippet;
	}

	let { children }: Props = $props();
</script>

<div class="relative min-h-screen overflow-hidden bg-background" role="presentation">
	<!-- Subtle Ambient Glows -->
	<div class="pointer-events-none absolute inset-0 overflow-hidden" aria-hidden="true">
		<div class="glow-xl-top-end"></div>
		<div class="glow-xl-bottom-start"></div>
	</div>

	<div
		class="relative z-10 flex min-h-screen flex-col justify-center px-4 py-8 sm:px-6 sm:py-12 lg:px-8"
	>
		{@render children()}
	</div>
</div>
