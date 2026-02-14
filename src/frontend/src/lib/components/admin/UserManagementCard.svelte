<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Badge } from '$lib/components/ui/badge';
	import { Button } from '$lib/components/ui/button';
	import * as Select from '$lib/components/ui/select';
	import * as Dialog from '$lib/components/ui/dialog';
	import { Separator } from '$lib/components/ui/separator';
	import { browserClient, getErrorMessage, isRateLimited } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { goto, invalidateAll } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { Plus, X, Loader2, Lock, Unlock, Trash2 } from '@lucide/svelte';
	import type { AdminUser, AdminRole, User } from '$lib/types';
	import {
		canManageUser,
		getAssignableRoles,
		getRoleRank,
		getHighestRank,
		hasPermission,
		Permissions
	} from '$lib/utils';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		user: AdminUser;
		roles: AdminRole[];
		currentUser: User;
	}

	let { user, roles, currentUser }: Props = $props();

	// --- Role management state ---
	let isAssigningRole = $state(false);
	let isRemovingRole = $state<string | null>(null);
	let selectedRole = $state('');

	// --- Account actions state ---
	let deleteDialogOpen = $state(false);
	let isLocking = $state(false);
	let isUnlocking = $state(false);
	let isDeleting = $state(false);

	// --- Derived permissions ---
	let callerRoles = $derived(currentUser.roles ?? []);
	let targetRoles = $derived(user.roles ?? []);
	let canManageByHierarchy = $derived(canManageUser(callerRoles, targetRoles));
	let canManage = $derived(
		canManageByHierarchy && hasPermission(currentUser, Permissions.Users.Manage)
	);
	let canAssignRoles = $derived(
		canManageByHierarchy && hasPermission(currentUser, Permissions.Users.AssignRoles)
	);
	let callerRank = $derived(getHighestRank(callerRoles));

	let availableRoles = $derived(
		getAssignableRoles(
			callerRoles,
			(roles ?? []).map((r) => r.name ?? '')
		).filter((role) => !targetRoles.includes(role))
	);

	function canRemoveRole(role: string): boolean {
		return canAssignRoles && getRoleRank(role) < callerRank;
	}

	// --- Role actions ---
	async function assignRole() {
		if (!selectedRole) return;
		isAssigningRole = true;
		const { response, error } = await browserClient.POST('/api/v1/admin/users/{id}/roles', {
			params: { path: { id: user.id ?? '' } },
			body: { role: selectedRole }
		});
		isAssigningRole = false;

		if (response.ok) {
			toast.success(m.admin_userDetail_roleAssigned());
			selectedRole = '';
			await invalidateAll();
		} else if (isRateLimited(response)) {
			toast.error(m.error_rateLimited(), { description: m.error_rateLimitedDescription() });
		} else {
			toast.error(getErrorMessage(error, m.admin_userDetail_roleAssignError()));
		}
	}

	async function removeRole(role: string) {
		isRemovingRole = role;
		const { response, error } = await browserClient.DELETE(
			'/api/v1/admin/users/{id}/roles/{role}',
			{
				params: { path: { id: user.id ?? '', role } }
			}
		);
		isRemovingRole = null;

		if (response.ok) {
			toast.success(m.admin_userDetail_roleRemoved());
			await invalidateAll();
		} else if (isRateLimited(response)) {
			toast.error(m.error_rateLimited(), { description: m.error_rateLimitedDescription() });
		} else {
			toast.error(getErrorMessage(error, m.admin_userDetail_roleRemoveError()));
		}
	}

	// --- Account actions ---
	async function lockUser() {
		isLocking = true;
		const { response, error } = await browserClient.POST('/api/v1/admin/users/{id}/lock', {
			params: { path: { id: user.id ?? '' } }
		});
		isLocking = false;

		if (response.ok) {
			toast.success(m.admin_userDetail_lockSuccess());
			await invalidateAll();
		} else if (isRateLimited(response)) {
			toast.error(m.error_rateLimited(), { description: m.error_rateLimitedDescription() });
		} else {
			toast.error(getErrorMessage(error, m.admin_userDetail_lockError()));
		}
	}

	async function unlockUser() {
		isUnlocking = true;
		const { response, error } = await browserClient.POST('/api/v1/admin/users/{id}/unlock', {
			params: { path: { id: user.id ?? '' } }
		});
		isUnlocking = false;

		if (response.ok) {
			toast.success(m.admin_userDetail_unlockSuccess());
			await invalidateAll();
		} else if (isRateLimited(response)) {
			toast.error(m.error_rateLimited(), { description: m.error_rateLimitedDescription() });
		} else {
			toast.error(getErrorMessage(error, m.admin_userDetail_unlockError()));
		}
	}

	async function deleteUser() {
		isDeleting = true;
		const { response, error } = await browserClient.DELETE('/api/v1/admin/users/{id}', {
			params: { path: { id: user.id ?? '' } }
		});
		isDeleting = false;
		deleteDialogOpen = false;

		if (response.ok) {
			toast.success(m.admin_userDetail_deleteSuccess());
			await goto(resolve('/admin/users'));
		} else if (isRateLimited(response)) {
			toast.error(m.error_rateLimited(), { description: m.error_rateLimitedDescription() });
		} else {
			toast.error(getErrorMessage(error, m.admin_userDetail_deleteError()));
		}
	}
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>{m.admin_userDetail_roleManagement()}</Card.Title>
		<Card.Description>{m.admin_userDetail_roleManagementDescription()}</Card.Description>
	</Card.Header>
	<Card.Content class="space-y-4">
		<!-- Current roles -->
		<div>
			<p class="mb-2 text-sm font-medium">{m.admin_userDetail_currentRoles()}</p>
			<div class="flex flex-wrap gap-2">
				{#each user.roles ?? [] as role (role)}
					<Badge variant="secondary" class="gap-1 py-1 text-sm">
						{role}
						{#if canRemoveRole(role)}
							<button
								class="ms-1 inline-flex h-5 w-5 items-center justify-center rounded-full transition-colors hover:bg-muted-foreground/20"
								aria-label="{m.admin_userDetail_removeRole()} {role}"
								disabled={isRemovingRole === role}
								onclick={() => removeRole(role)}
							>
								{#if isRemovingRole === role}
									<Loader2 class="h-3 w-3 animate-spin" />
								{:else}
									<X class="h-3 w-3" />
								{/if}
							</button>
						{/if}
					</Badge>
				{:else}
					<span class="text-sm text-muted-foreground">{m.admin_userDetail_noRoles()}</span>
				{/each}
			</div>
		</div>

		<!-- Assign role -->
		{#if canAssignRoles && availableRoles.length > 0}
			<div class="flex flex-col gap-2 sm:flex-row sm:items-end">
				<div class="flex-1">
					<label for="role-select" class="mb-1 block text-sm font-medium">
						{m.admin_userDetail_assignRole()}
					</label>
					<Select.Root type="single" bind:value={selectedRole}>
						<Select.Trigger id="role-select">
							{#if selectedRole}
								<span>{selectedRole}</span>
							{:else}
								<span class="text-muted-foreground">{m.admin_userDetail_selectRole()}</span>
							{/if}
						</Select.Trigger>
						<Select.Content>
							{#each availableRoles as role (role)}
								<Select.Item value={role}>{role}</Select.Item>
							{/each}
						</Select.Content>
					</Select.Root>
				</div>
				<Button
					size="sm"
					class="min-h-10 shrink-0"
					disabled={!selectedRole || isAssigningRole}
					onclick={assignRole}
				>
					{#if isAssigningRole}
						<Loader2 class="me-1 h-4 w-4 animate-spin" />
					{:else}
						<Plus class="me-1 h-4 w-4" />
					{/if}
					{m.admin_userDetail_assignRole()}
				</Button>
			</div>
		{/if}

		<!-- Account actions -->
		{#if canManage}
			<Separator />

			<div class="flex flex-wrap items-center gap-2">
				{#if user.isLockedOut}
					<Button
						variant="outline"
						size="sm"
						class="min-h-10"
						disabled={isUnlocking}
						onclick={unlockUser}
					>
						{#if isUnlocking}
							<Loader2 class="me-2 h-4 w-4 animate-spin" />
						{:else}
							<Unlock class="me-2 h-4 w-4" />
						{/if}
						{m.admin_userDetail_unlockAccount()}
					</Button>
				{:else}
					<Button
						variant="outline"
						size="sm"
						class="min-h-10"
						disabled={isLocking}
						onclick={lockUser}
					>
						{#if isLocking}
							<Loader2 class="me-2 h-4 w-4 animate-spin" />
						{:else}
							<Lock class="me-2 h-4 w-4" />
						{/if}
						{m.admin_userDetail_lockAccount()}
					</Button>
				{/if}

				<Dialog.Root bind:open={deleteDialogOpen}>
					<Dialog.Trigger>
						{#snippet child({ props })}
							<Button variant="destructive" size="sm" class="min-h-10" {...props}>
								<Trash2 class="me-2 h-4 w-4" />
								{m.admin_userDetail_deleteAccount()}
							</Button>
						{/snippet}
					</Dialog.Trigger>
					<Dialog.Content>
						<Dialog.Header>
							<Dialog.Title>{m.admin_userDetail_deleteConfirmTitle()}</Dialog.Title>
							<Dialog.Description>
								{m.admin_userDetail_deleteConfirmDescription()}
							</Dialog.Description>
						</Dialog.Header>
						<Dialog.Footer class="flex-col-reverse sm:flex-row">
							<Button variant="outline" onclick={() => (deleteDialogOpen = false)}>
								{m.common_cancel()}
							</Button>
							<Button variant="destructive" disabled={isDeleting} onclick={deleteUser}>
								{#if isDeleting}
									<Loader2 class="me-2 h-4 w-4 animate-spin" />
								{/if}
								{m.common_delete()}
							</Button>
						</Dialog.Footer>
					</Dialog.Content>
				</Dialog.Root>
			</div>
		{:else}
			<Separator />
			<p class="text-sm text-muted-foreground">
				{m.admin_userDetail_cannotManage()}
			</p>
		{/if}
	</Card.Content>
</Card.Root>
