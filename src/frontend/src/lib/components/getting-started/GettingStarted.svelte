<!--
╔══════════════════════════════════════════════════════════════════════════════╗
║                        🚀 GETTING STARTED COMPONENT                           ║
║                                                                              ║
║  This component is designed to be REMOVED once you start building your app.  ║
║                                                                              ║
║  TO REMOVE:                                                                  ║
║  ──────────                                                                  ║
║  1. Delete this folder: src/lib/components/getting-started/                  ║
║  2. Update src/routes/(app)/+page.svelte to render your own dashboard        ║
║  3. Remove translation keys from src/messages/*.json:                        ║
║     - All keys starting with "gettingStarted_*"                              ║
║     - Keys: dashboard_authenticated, dashboard_welcome_*,                    ║
║             dashboard_feature_*, dashboard_guide_*                           ║
║  4. Update meta tags (meta_dashboard_*) with your own content                ║
║                                                                              ║
╚══════════════════════════════════════════════════════════════════════════════╝
-->

<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import * as Dialog from '$lib/components/ui/dialog';
	import { Button } from '$lib/components/ui/button';
	import { StatusIndicator } from '$lib/components/common';
	import * as m from '$lib/paraglide/messages';
	import {
		ShieldCheck,
		Globe,
		Layout,
		FileCode,
		Rocket,
		Sparkles,
		Code2,
		FolderTree,
		Trash2,
		Languages,
		Terminal,
		BookOpen,
		CheckCircle2,
		Copy,
		Zap,
		Palette,
		Server,
		ExternalLink
	} from '@lucide/svelte';
	import { fly, scale } from 'svelte/transition';
	import { cubicOut, elasticOut, backOut } from 'svelte/easing';
	import { onMount } from 'svelte';
	import { renderMarkdown } from './markdown';
	import readmeContent from '../../../../README.md?raw';
	import copilotInstructions from '../../../../.github/copilot-instructions.md?raw';

	interface Props {
		username?: string;
	}

	let { username }: Props = $props();

	let mounted = $state(false);
	let copiedCommand = $state<string | null>(null);
	let readmeDialogOpen = $state(false);
	let copilotDialogOpen = $state(false);

	onMount(() => {
		mounted = true;
	});

	async function copyToClipboard(text: string, id: string) {
		await navigator.clipboard.writeText(text);
		copiedCommand = id;
		setTimeout(() => (copiedCommand = null), 2000);
	}

	const features = [
		{
			title: m.dashboard_feature_auth_title,
			desc: m.dashboard_feature_auth_description,
			icon: ShieldCheck,
			gradient: 'from-blue-500 to-cyan-500',
			delay: 0
		},
		{
			title: m.dashboard_feature_i18n_title,
			desc: m.dashboard_feature_i18n_description,
			icon: Globe,
			gradient: 'from-green-500 to-emerald-500',
			delay: 100
		},
		{
			title: m.dashboard_feature_ui_title,
			desc: m.dashboard_feature_ui_description,
			icon: Layout,
			gradient: 'from-purple-500 to-pink-500',
			delay: 200
		},
		{
			title: m.dashboard_feature_api_title,
			desc: m.dashboard_feature_api_description,
			icon: FileCode,
			gradient: 'from-orange-500 to-red-500',
			delay: 300
		}
	];

	const techStack = [
		{ name: 'SvelteKit', icon: Zap, color: 'text-orange-500' },
		{ name: 'TypeScript', icon: Code2, color: 'text-blue-500' },
		{ name: 'Tailwind CSS', icon: Palette, color: 'text-cyan-500' },
		{ name: 'ASP.NET Core', icon: Server, color: 'text-purple-500' }
	];

	const quickCommands = [
		{ label: m.gettingStarted_cmd_dev, cmd: 'npm run dev', id: 'dev' },
		{ label: m.gettingStarted_cmd_apiGen, cmd: 'npm run api:generate', id: 'api' },
		{
			label: m.gettingStarted_cmd_addComponent,
			cmd: 'npx shadcn-svelte@next add button',
			id: 'shadcn'
		},
		{
			label: m.gettingStarted_cmd_check,
			cmd: 'npm run format && npm run lint && npm run check',
			id: 'check'
		}
	];

	const removalSteps = [
		{
			icon: FolderTree,
			title: m.gettingStarted_removal_routes_title,
			desc: m.gettingStarted_removal_routes_desc
		},
		{
			icon: Languages,
			title: m.gettingStarted_removal_i18n_title,
			desc: m.gettingStarted_removal_i18n_desc
		},
		{
			icon: Code2,
			title: m.gettingStarted_removal_nav_title,
			desc: m.gettingStarted_removal_nav_desc
		}
	];
</script>

<div class="relative space-y-6 pb-6 sm:space-y-8 sm:pb-8">
	<!-- Hero Section -->
	{#if mounted}
		<div
			class="relative overflow-hidden rounded-xl border bg-gradient-to-br from-background via-background to-muted/50 p-4 sm:rounded-2xl sm:p-6 md:p-8 lg:p-12"
			in:fly={{ y: 30, duration: 600, easing: cubicOut }}
		>
			<!-- Decorative Elements -->
			<div class="absolute end-4 top-4 flex gap-2 opacity-50">
				<div class="h-3 w-3 rounded-full bg-red-500"></div>
				<div class="h-3 w-3 rounded-full bg-yellow-500"></div>
				<div class="h-3 w-3 rounded-full bg-green-500"></div>
			</div>

			<div class="flex flex-col gap-4 sm:gap-6 lg:flex-row lg:items-center lg:justify-between">
				<div class="space-y-3 sm:space-y-4">
					<div
						class="inline-flex items-center gap-2 rounded-full border bg-muted/50 px-4 py-1.5 text-sm"
						in:scale={{ duration: 400, delay: 200, easing: elasticOut }}
					>
						<Sparkles class="h-4 w-4 text-yellow-500" />
						<span>{m.gettingStarted_badge()}</span>
					</div>

					<h1
						class="text-2xl font-bold tracking-tight sm:text-3xl md:text-4xl lg:text-5xl"
						in:fly={{ y: 20, duration: 500, delay: 100, easing: cubicOut }}
					>
						<span
							class="bg-gradient-to-r from-foreground via-foreground to-muted-foreground bg-clip-text"
						>
							{m.dashboard_welcome_title()}
						</span>
					</h1>

					<p
						class="max-w-xl text-sm text-muted-foreground sm:text-base md:text-lg"
						in:fly={{ y: 20, duration: 500, delay: 200, easing: cubicOut }}
					>
						{m.dashboard_welcome_description()}
					</p>

					<!-- Tech Stack Pills -->
					<div
						class="flex flex-wrap gap-2 pt-2"
						in:fly={{ y: 20, duration: 500, delay: 300, easing: cubicOut }}
					>
						{#each techStack as tech, i (tech.name)}
							<div
								class="inline-flex items-center gap-1.5 rounded-full border bg-background/80 px-3 py-1 text-sm backdrop-blur-sm transition-transform hover:scale-105"
								in:scale={{ duration: 300, delay: 400 + i * 50, easing: backOut }}
							>
								<tech.icon class="h-3.5 w-3.5 {tech.color}" />
								<span>{tech.name}</span>
							</div>
						{/each}
					</div>
				</div>

				<!-- User Status Card -->
				<div class="shrink-0" in:scale={{ duration: 400, delay: 400, easing: backOut }}>
					<div
						class="relative overflow-hidden rounded-xl border border-success/30 bg-gradient-to-br from-success/10 to-emerald-500/10 p-6 backdrop-blur-sm"
					>
						<div class="glow-success"></div>
						<div class="relative space-y-3">
							<StatusIndicator status="online" icon={CheckCircle2} size="lg">
								<span class="font-semibold text-green-600 dark:text-green-400">
									{m.dashboard_authenticated()}
								</span>
							</StatusIndicator>
							<div class="text-sm text-muted-foreground">
								{m.nav_loggedInAs()}
							</div>
							<div class="font-mono text-sm font-medium">{username}</div>
						</div>
					</div>
				</div>
			</div>
		</div>
	{/if}

	<!-- Features Grid -->
	{#if mounted}
		<div class="space-y-3 sm:space-y-4">
			<h2
				class="flex items-center gap-2 text-lg font-semibold sm:text-xl"
				in:fly={{ y: 20, duration: 500, delay: 500, easing: cubicOut }}
			>
				<Rocket class="h-4 w-4 text-primary sm:h-5 sm:w-5" />
				{m.gettingStarted_features_title()}
			</h2>

			<div class="grid gap-3 sm:grid-cols-2 sm:gap-4 xl:grid-cols-4">
				{#each features as feature (feature.gradient)}
					<div in:fly={{ y: 30, duration: 500, delay: 600 + feature.delay, easing: cubicOut }}>
						<Card.Root
							class="group relative h-full overflow-hidden transition-all duration-300 hover:-translate-y-1 hover:shadow-lg hover:shadow-primary/5"
						>
							<!-- Gradient Background on Hover -->
							<div
								class="absolute inset-0 bg-gradient-to-br {feature.gradient} opacity-0 transition-opacity duration-300 group-hover:opacity-5"
							></div>

							<Card.Header class="relative pb-2">
								<div
									class="mb-2 inline-flex rounded-lg bg-gradient-to-br {feature.gradient} p-2.5 text-white shadow-lg transition-transform duration-300 group-hover:scale-110"
								>
									<feature.icon class="h-5 w-5" />
								</div>
								<Card.Title class="text-base">{feature.title()}</Card.Title>
							</Card.Header>
							<Card.Content class="relative">
								<p class="text-sm leading-relaxed text-muted-foreground">
									{feature.desc()}
								</p>
							</Card.Content>
						</Card.Root>
					</div>
				{/each}
			</div>
		</div>
	{/if}

	<!-- Quick Commands Section -->
	{#if mounted}
		<div
			class="space-y-3 sm:space-y-4"
			in:fly={{ y: 30, duration: 500, delay: 1000, easing: cubicOut }}
		>
			<h2 class="flex items-center gap-2 text-lg font-semibold sm:text-xl">
				<Terminal class="h-4 w-4 text-primary sm:h-5 sm:w-5" />
				{m.gettingStarted_commands_title()}
			</h2>

			<div class="grid gap-2 sm:gap-3 md:grid-cols-2">
				{#each quickCommands as command (command.id)}
					<button
						onclick={() => copyToClipboard(command.cmd, command.id)}
						class="group relative flex items-center justify-between gap-2 overflow-hidden rounded-lg border bg-muted/30 p-3 text-start transition-all hover:border-primary/50 hover:bg-muted/50 sm:gap-4 sm:p-4"
					>
						<div class="min-w-0 flex-1 space-y-1 overflow-hidden">
							<div class="text-xs text-muted-foreground sm:text-sm">{command.label()}</div>
							<code class="block truncate font-mono text-xs sm:text-sm">{command.cmd}</code>
						</div>
						<div class="shrink-0">
							{#if copiedCommand === command.id}
								<div in:scale={{ duration: 200 }}>
									<CheckCircle2 class="h-5 w-5 text-green-500" />
								</div>
							{:else}
								<Copy
									class="h-5 w-5 text-muted-foreground transition-colors group-hover:text-foreground"
								/>
							{/if}
						</div>
					</button>
				{/each}
			</div>
		</div>
	{/if}

	<!-- How to Remove This Page Section -->
	{#if mounted}
		<div in:fly={{ y: 30, duration: 500, delay: 1200, easing: cubicOut }}>
			<Card.Root
				class="relative overflow-hidden border-2 border-dashed border-destructive/30 bg-destructive/5"
			>
				<div
					class="absolute -end-20 -top-20 h-40 w-40 rounded-full bg-destructive/10 blur-3xl"
				></div>

				<Card.Header class="relative">
					<div class="flex items-start gap-3 sm:items-center">
						<div class="shrink-0 rounded-lg bg-destructive/10 p-2">
							<Trash2 class="h-4 w-4 text-destructive sm:h-5 sm:w-5" />
						</div>
						<div class="min-w-0">
							<Card.Title class="text-base sm:text-lg"
								>{m.gettingStarted_removal_title()}</Card.Title
							>
							<Card.Description class="text-xs sm:text-sm"
								>{m.gettingStarted_removal_subtitle()}</Card.Description
							>
						</div>
					</div>
				</Card.Header>

				<Card.Content class="relative space-y-4 sm:space-y-6">
					<div class="grid gap-3 sm:gap-4 lg:grid-cols-3">
						{#each removalSteps as step, i (i)}
							<div
								class="space-y-2 rounded-lg border border-destructive/20 bg-background/50 p-3 sm:p-4"
								in:fly={{ y: 20, duration: 400, delay: 1300 + i * 100, easing: cubicOut }}
							>
								<div class="flex items-center gap-2">
									<div
										class="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-destructive/10 text-xs font-bold text-destructive sm:h-6 sm:w-6"
									>
										{i + 1}
									</div>
									<step.icon class="h-3.5 w-3.5 text-destructive/70 sm:h-4 sm:w-4" />
								</div>
								<h3 class="text-sm font-medium sm:text-base">{step.title()}</h3>
								<p class="text-xs text-muted-foreground sm:text-sm">{step.desc()}</p>
							</div>
						{/each}
					</div>

					<!-- Code Snippet -->
					<div class="rounded-lg border bg-muted p-3 sm:p-4">
						<div class="mb-2 flex items-center gap-2 text-xs text-muted-foreground">
							<div class="h-2 w-2 rounded-full bg-muted-foreground/50"></div>
							<span>{m.gettingStarted_removal_i18nKeys()}</span>
						</div>
						<pre class="overflow-x-auto text-xs sm:text-sm"><code class="text-foreground"
								>{`// src/messages/en.json & cs.json
// Remove these key patterns:
"dashboard_*"         // All dashboard keys
"gettingStarted_*"    // All getting started keys`}</code
							></pre>
					</div>
				</Card.Content>
			</Card.Root>
		</div>
	{/if}

	<!-- Documentation Link -->
	{#if mounted}
		<div
			class="flex flex-col items-center justify-center gap-3 rounded-xl border bg-muted/30 p-4 text-center sm:gap-4 sm:p-6 md:p-8"
			in:fly={{ y: 30, duration: 500, delay: 1400, easing: cubicOut }}
		>
			<BookOpen class="h-6 w-6 text-muted-foreground sm:h-8 sm:w-8" />
			<div class="space-y-1">
				<h3 class="text-sm font-semibold sm:text-base">{m.gettingStarted_docs_title()}</h3>
				<p class="text-xs text-muted-foreground sm:text-sm">
					{m.gettingStarted_docs_description()}
				</p>
			</div>
			<div class="flex w-full flex-col gap-2 sm:w-auto sm:flex-row sm:gap-3">
				<Button variant="outline" class="gap-2" size="sm" onclick={() => (readmeDialogOpen = true)}>
					<code class="text-xs">README.md</code>
					<ExternalLink class="h-3.5 w-3.5" />
				</Button>
				<Button
					variant="outline"
					class="gap-2"
					size="sm"
					onclick={() => (copilotDialogOpen = true)}
				>
					<code class="text-xs">copilot-instructions.md</code>
					<ExternalLink class="h-3.5 w-3.5" />
				</Button>
			</div>
		</div>
	{/if}
</div>

<!-- README Dialog -->
<Dialog.Root bind:open={readmeDialogOpen}>
	<Dialog.Content
		class="max-h-[90vh] w-[95vw] max-w-sm overflow-hidden p-0 sm:max-w-2xl md:max-w-4xl lg:max-w-5xl"
	>
		<Dialog.Header class="border-b px-4 py-3 sm:px-6 sm:py-4">
			<Dialog.Title class="flex items-center gap-2 text-sm sm:text-base">
				<BookOpen class="h-4 w-4 sm:h-5 sm:w-5" />
				README.md
			</Dialog.Title>
			<Dialog.Description class="sr-only">Project documentation</Dialog.Description>
		</Dialog.Header>
		<div
			class="prose prose-sm dark:prose-invert md:prose-base max-h-[calc(90vh-60px)] overflow-y-auto px-4 py-3 sm:max-h-[calc(90vh-70px)] sm:px-6 sm:py-4"
		>
			<!-- eslint-disable-next-line svelte/no-at-html-tags -- Rendering our own README, not user input -->
			{@html renderMarkdown(readmeContent)}
		</div>
	</Dialog.Content>
</Dialog.Root>

<!-- Copilot Instructions Dialog -->
<Dialog.Root bind:open={copilotDialogOpen}>
	<Dialog.Content
		class="max-h-[90vh] w-[95vw] max-w-sm overflow-hidden p-0 sm:max-w-2xl md:max-w-4xl lg:max-w-5xl"
	>
		<Dialog.Header class="border-b px-4 py-3 sm:px-6 sm:py-4">
			<Dialog.Title class="flex items-center gap-2 text-sm sm:text-base">
				<BookOpen class="h-4 w-4 sm:h-5 sm:w-5" />
				copilot-instructions.md
			</Dialog.Title>
			<Dialog.Description class="sr-only">GitHub Copilot coding instructions</Dialog.Description>
		</Dialog.Header>
		<div
			class="prose prose-sm dark:prose-invert md:prose-base max-h-[calc(90vh-60px)] overflow-y-auto px-4 py-3 sm:max-h-[calc(90vh-70px)] sm:px-6 sm:py-4"
		>
			<!-- eslint-disable-next-line svelte/no-at-html-tags -- Rendering our own instructions, not user input -->
			{@html renderMarkdown(copilotInstructions)}
		</div>
	</Dialog.Content>
</Dialog.Root>
