<script lang="ts">
	import { ChangePasswordForm, DeleteAccountDialog } from '$lib/components/settings';
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import * as m from '$lib/paraglide/messages';

	let deleteDialogOpen = $state(false);
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_settings_title() })}</title>
	<meta name="description" content={m.meta_settings_description()} />
</svelte:head>

<div class="space-y-6">
	<div>
		<h3 class="text-lg font-medium">{m.settings_title()}</h3>
		<p class="text-sm text-muted-foreground">{m.settings_description()}</p>
	</div>
	<div class="h-px w-full bg-border"></div>
	<div class="max-w-2xl space-y-12">
		<ChangePasswordForm />

		<div class="space-y-4">
			<h4 class="text-sm font-medium text-destructive">{m.settings_deleteAccount_dangerZone()}</h4>
			<Card.Root class="border-destructive/50">
				<Card.Header>
					<Card.Title class="text-destructive">{m.settings_deleteAccount_title()}</Card.Title>
					<Card.Description>
						{m.settings_deleteAccount_dangerDescription()}
					</Card.Description>
				</Card.Header>
				<Card.Content>
					<div class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
						<div>
							<p class="text-sm font-medium">{m.settings_deleteAccount_title()}</p>
							<p class="text-sm text-muted-foreground">
								{m.settings_deleteAccount_description()}
							</p>
						</div>
						<Button
							variant="destructive"
							class="shrink-0"
							onclick={() => (deleteDialogOpen = true)}
						>
							{m.settings_deleteAccount_title()}
						</Button>
					</div>
				</Card.Content>
			</Card.Root>
		</div>
	</div>
</div>

<DeleteAccountDialog bind:open={deleteDialogOpen} />
