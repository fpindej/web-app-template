<script lang="ts">
	import Navbar from '$lib/components/layout/Navbar.svelte';

	let { children, data } = $props();
</script>

<div class="min-h-screen bg-background">
	<Navbar user={data.user} />

	<main class="py-10">
		<div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
			{@render children()}
		</div>
	</main>
</div>
