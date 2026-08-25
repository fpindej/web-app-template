<script lang="ts">
	import type { Snippet } from 'svelte';
	import { cn } from '$lib/utils';
	import { ThemeToggle, LanguageSelector } from '$lib/components/layout';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		children: Snippet;
		extras?: Snippet;
		cardClass?: string;
		success?: boolean;
	}

	let { children, extras, cardClass, success = false }: Props = $props();
</script>

<div class="flex min-h-svh flex-col items-center justify-center bg-muted p-6 md:p-10">
	{#if extras}
		{@render extras()}
	{/if}

	<div
		class={cn(
			'flex w-full max-w-sm flex-col gap-6 lg:max-w-3xl',
			success ? 'animate-auth-exit' : 'animate-auth-enter'
		)}
	>
		<div class="flex items-center justify-end gap-2">
			<LanguageSelector />
			<ThemeToggle />
		</div>
		<div
			class={cn(
				'overflow-hidden rounded-xl border bg-card text-card-foreground shadow-sm transition-colors duration-300',
				cardClass
			)}
		>
			<div class="grid lg:grid-cols-2">
				<div class="p-6 lg:p-8">
					{@render children()}
				</div>
				<div class="relative hidden overflow-hidden bg-primary/5 lg:block" aria-hidden="true">
					<div class="pointer-events-none absolute inset-0">
						<div class="dot-grid-fade"></div>
						<div class="glow-xl-top-end"></div>
						<div class="glow-xl-bottom-start"></div>
					</div>
					<div class="flex h-full flex-col items-center justify-center gap-2 p-8 text-center">
						<h2 class="text-2xl font-bold tracking-tight text-foreground/80">
							{m.app_name()}
						</h2>
						<p class="text-sm text-balance text-muted-foreground">
							{m.auth_shell_tagline()}
						</p>
					</div>
				</div>
			</div>
		</div>
	</div>
</div>
