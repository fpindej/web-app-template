<script lang="ts">
	import { Header, Sidebar } from '$lib/components/layout';

	let { children, data } = $props();
</script>

<div
	class="grid h-dvh w-full md:grid-cols-[var(--sidebar-width-md)_1fr] lg:grid-cols-[var(--sidebar-width-lg)_1fr]"
>
	<div class="hidden border-r bg-muted/40 md:block">
		<Sidebar class="h-full" user={data.user} />
	</div>
	<div class="flex flex-col overflow-hidden">
		<Header user={data.user} />
		<main class="flex flex-1 flex-col gap-4 overflow-y-auto p-4 lg:gap-6 lg:p-6">
			{@render children()}
		</main>
	</div>
</div>
