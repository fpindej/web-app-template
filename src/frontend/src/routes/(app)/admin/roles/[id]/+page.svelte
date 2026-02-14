<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Badge } from '$lib/components/ui/badge';
	import * as Dialog from '$lib/components/ui/dialog';
	import { RolePermissionEditor } from '$lib/components/admin';
	import { ArrowLeft, Loader2, Save, Trash2 } from '@lucide/svelte';
	import { browserClient, getErrorMessage, isRateLimited, getRetryAfterSeconds } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { goto, invalidateAll } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { createCooldown } from '$lib/state';
	import { hasPermission, Permissions } from '$lib/utils';
	import * as m from '$lib/paraglide/messages';
	import type { PageData } from './$types';

	let { data }: { data: PageData } = $props();

	let canManageRoles = $derived(hasPermission(data.user, Permissions.Roles.Manage));
	let isSuperAdmin = $derived(data.role?.name === 'SuperAdmin');
	let isSystem = $derived(data.role?.isSystem ?? false);
	let canEditPermissions = $derived(canManageRoles && !isSuperAdmin);
	let canEditName = $derived(canManageRoles && !isSystem);
	let canDelete = $derived(canManageRoles && !isSystem && (data.role?.userCount ?? 0) === 0);

	let roleName = $state(data.role?.name ?? '');
	let roleDescription = $state(data.role?.description ?? '');
	let selectedPermissions = $state<string[]>(data.role?.permissions ?? []);

	let isSaving = $state(false);
	let isSavingPermissions = $state(false);
	let deleteDialogOpen = $state(false);
	let isDeleting = $state(false);
	const cooldown = createCooldown();

	function handleRateLimited(response: Response) {
		const retryAfter = getRetryAfterSeconds(response);
		if (retryAfter) cooldown.start(retryAfter);
		toast.error(m.error_rateLimited(), {
			description: retryAfter
				? m.error_rateLimitedDescriptionWithRetry({ seconds: retryAfter })
				: m.error_rateLimitedDescription()
		});
	}

	async function saveRole() {
		isSaving = true;
		const { response, error } = await browserClient.PUT('/api/v1/admin/roles/{id}', {
			params: { path: { id: data.role?.id ?? '' } },
			body: {
				name: canEditName ? roleName : null,
				description: roleDescription
			}
		});
		isSaving = false;

		if (response.ok) {
			toast.success(m.admin_roles_updateSuccess());
			await invalidateAll();
		} else if (isRateLimited(response)) {
			handleRateLimited(response);
		} else {
			toast.error(getErrorMessage(error, m.admin_roles_updateError()));
		}
	}

	async function savePermissions() {
		isSavingPermissions = true;
		const { response, error } = await browserClient.PUT('/api/v1/admin/roles/{id}/permissions', {
			params: { path: { id: data.role?.id ?? '' } },
			body: { permissions: selectedPermissions }
		});
		isSavingPermissions = false;

		if (response.ok) {
			toast.success(m.admin_roles_permissionsSaved());
			await invalidateAll();
		} else if (isRateLimited(response)) {
			handleRateLimited(response);
		} else {
			toast.error(getErrorMessage(error, m.admin_roles_permissionsSaveError()));
		}
	}

	async function deleteRole() {
		isDeleting = true;
		const { response, error } = await browserClient.DELETE('/api/v1/admin/roles/{id}', {
			params: { path: { id: data.role?.id ?? '' } }
		});
		isDeleting = false;
		deleteDialogOpen = false;

		if (response.ok) {
			toast.success(m.admin_roles_deleteSuccess());
			await goto(resolve('/admin/roles'));
		} else if (isRateLimited(response)) {
			handleRateLimited(response);
		} else {
			toast.error(getErrorMessage(error, m.admin_roles_deleteError()));
		}
	}
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: data.role?.name ?? m.meta_adminRoles_title() })}</title>
</svelte:head>

<div class="space-y-6">
	<!-- eslint-disable svelte/no-navigation-without-resolve -- href is pre-resolved -->
	<a
		href={resolve('/admin/roles')}
		class="inline-flex items-center text-sm text-muted-foreground hover:text-foreground"
	>
		<ArrowLeft class="me-1 h-4 w-4" />
		{m.admin_roles_backToRoles()}
	</a>

	<div class="flex items-center gap-3">
		<h3 class="text-lg font-medium">{data.role?.name}</h3>
		{#if isSystem}
			<Badge variant="outline">{m.admin_roles_system()}</Badge>
		{/if}
		<span class="text-sm text-muted-foreground">
			{m.admin_roles_userCountLabel({ count: data.role?.userCount ?? 0 })}
		</span>
	</div>
	<div class="h-px w-full bg-border"></div>

	<!-- Role details -->
	<Card.Root>
		<Card.Header>
			<Card.Title>{m.admin_roles_detailTitle()}</Card.Title>
			<Card.Description>{m.admin_roles_detailDescription()}</Card.Description>
		</Card.Header>
		<Card.Content class="space-y-4">
			<div>
				<label for="role-name" class="mb-1 block text-sm font-medium">
					{m.admin_roles_name()}
				</label>
				<Input id="role-name" bind:value={roleName} disabled={!canEditName} maxlength={50} />
				{#if isSystem}
					<p class="mt-1 text-xs text-muted-foreground">{m.admin_roles_systemNameReadonly()}</p>
				{/if}
			</div>
			<div>
				<label for="role-desc" class="mb-1 block text-sm font-medium">
					{m.admin_roles_descriptionLabel()}
				</label>
				<Input
					id="role-desc"
					bind:value={roleDescription}
					disabled={!canManageRoles}
					maxlength={200}
					placeholder={m.admin_roles_descriptionPlaceholder()}
				/>
			</div>
			{#if canManageRoles}
				<Button size="sm" disabled={isSaving || cooldown.active} onclick={saveRole}>
					{#if isSaving}
						<Loader2 class="me-2 h-4 w-4 animate-spin" />
					{:else}
						<Save class="me-2 h-4 w-4" />
					{/if}
					{m.admin_roles_saveDetails()}
				</Button>
			{/if}
		</Card.Content>
	</Card.Root>

	<!-- Permissions -->
	<Card.Root>
		<Card.Header>
			<Card.Title>{m.admin_roles_permissionsTitle()}</Card.Title>
			<Card.Description>{m.admin_roles_permissionsDescription()}</Card.Description>
		</Card.Header>
		<Card.Content class="space-y-4">
			<RolePermissionEditor
				permissionGroups={data.permissionGroups}
				selected={selectedPermissions}
				disabled={!canEditPermissions}
				onchange={(perms) => (selectedPermissions = perms)}
			/>
			{#if canEditPermissions}
				<Button
					size="sm"
					disabled={isSavingPermissions || cooldown.active}
					onclick={savePermissions}
				>
					{#if isSavingPermissions}
						<Loader2 class="me-2 h-4 w-4 animate-spin" />
					{:else}
						<Save class="me-2 h-4 w-4" />
					{/if}
					{m.admin_roles_savePermissions()}
				</Button>
			{/if}
		</Card.Content>
	</Card.Root>

	<!-- Danger zone: delete -->
	{#if canDelete}
		<Card.Root class="border-destructive">
			<Card.Header>
				<Card.Title>{m.admin_userDetail_dangerZone()}</Card.Title>
			</Card.Header>
			<Card.Content>
				<Dialog.Root bind:open={deleteDialogOpen}>
					<Dialog.Trigger>
						{#snippet child({ props })}
							<Button variant="destructive" size="sm" {...props}>
								<Trash2 class="me-2 h-4 w-4" />
								{m.admin_roles_deleteRole()}
							</Button>
						{/snippet}
					</Dialog.Trigger>
					<Dialog.Content>
						<Dialog.Header>
							<Dialog.Title>{m.admin_roles_deleteConfirmTitle()}</Dialog.Title>
							<Dialog.Description>
								{m.admin_roles_deleteConfirmDescription()}
							</Dialog.Description>
						</Dialog.Header>
						<Dialog.Footer class="flex-col-reverse sm:flex-row">
							<Button variant="outline" onclick={() => (deleteDialogOpen = false)}>
								{m.common_cancel()}
							</Button>
							<Button
								variant="destructive"
								disabled={isDeleting || cooldown.active}
								onclick={deleteRole}
							>
								{#if isDeleting}
									<Loader2 class="me-2 h-4 w-4 animate-spin" />
								{/if}
								{m.common_delete()}
							</Button>
						</Dialog.Footer>
					</Dialog.Content>
				</Dialog.Root>
			</Card.Content>
		</Card.Root>
	{/if}
</div>
