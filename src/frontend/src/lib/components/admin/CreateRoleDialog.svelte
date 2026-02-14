<script lang="ts">
	import * as Dialog from '$lib/components/ui/dialog';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Loader2 } from '@lucide/svelte';
	import { browserClient, getErrorMessage } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { invalidateAll } from '$app/navigation';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		open: boolean;
	}

	let { open = $bindable() }: Props = $props();

	let name = $state('');
	let description = $state('');
	let isCreating = $state(false);

	async function createRole() {
		if (!name.trim()) return;
		isCreating = true;

		const { response, error } = await browserClient.POST('/api/v1/admin/roles', {
			body: { name: name.trim(), description: description.trim() || null }
		});

		isCreating = false;

		if (response.ok) {
			toast.success(m.admin_roles_createSuccess());
			name = '';
			description = '';
			open = false;
			await invalidateAll();
		} else {
			toast.error(getErrorMessage(error, m.admin_roles_createError()));
		}
	}
</script>

<Dialog.Root bind:open>
	<Dialog.Content>
		<Dialog.Header>
			<Dialog.Title>{m.admin_roles_createRole()}</Dialog.Title>
			<Dialog.Description>{m.admin_roles_createRoleDescription()}</Dialog.Description>
		</Dialog.Header>
		<div class="space-y-4 py-4">
			<div>
				<label for="role-name" class="mb-1 block text-sm font-medium">
					{m.admin_roles_name()}
				</label>
				<Input
					id="role-name"
					bind:value={name}
					placeholder={m.admin_roles_namePlaceholder()}
					maxlength={50}
				/>
			</div>
			<div>
				<label for="role-description" class="mb-1 block text-sm font-medium">
					{m.admin_roles_descriptionLabel()}
				</label>
				<Input
					id="role-description"
					bind:value={description}
					placeholder={m.admin_roles_descriptionPlaceholder()}
					maxlength={200}
				/>
			</div>
		</div>
		<Dialog.Footer class="flex-col-reverse sm:flex-row">
			<Button variant="outline" onclick={() => (open = false)}>
				{m.common_cancel()}
			</Button>
			<Button disabled={!name.trim() || isCreating} onclick={createRole}>
				{#if isCreating}
					<Loader2 class="me-2 h-4 w-4 animate-spin" />
				{/if}
				{m.admin_roles_createRole()}
			</Button>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>
