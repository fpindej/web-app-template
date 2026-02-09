<script lang="ts">
	import * as Card from '$lib/components/ui/card';
	import { Badge } from '$lib/components/ui/badge';
	import { Button } from '$lib/components/ui/button';
	import * as Dialog from '$lib/components/ui/dialog';
	import { browserClient, getErrorMessage } from '$lib/api';
	import { toast } from '$lib/components/ui/sonner';
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { invalidateAll } from '$app/navigation';
	import {
		Shield,
		Lock,
		Unlock,
		Trash2,
		Plus,
		X,
		User as UserIcon,
		Mail,
		Phone,
		Hash,
		CheckCircle,
		XCircle
	} from '@lucide/svelte';
	import type { AdminUser, AdminRole, User } from '$lib/types';
	import { canManageUser, getAssignableRoles, getRoleRank, getHighestRank } from '$lib/utils';
	import * as m from '$lib/paraglide/messages';

	interface Props {
		user: AdminUser;
		roles: AdminRole[];
		currentUser: User;
	}

	let { user, roles, currentUser }: Props = $props();

	let deleteDialogOpen = $state(false);
	let isLocking = $state(false);
	let isUnlocking = $state(false);
	let isDeleting = $state(false);
	let isAssigningRole = $state(false);
	let isRemovingRole = $state<string | null>(null);

	let callerRoles = $derived(currentUser.roles ?? []);
	let targetRoles = $derived(user.roles ?? []);
	let canManage = $derived(canManageUser(callerRoles, targetRoles));
	let callerRank = $derived(getHighestRank(callerRoles));

	let availableRoles = $derived(
		getAssignableRoles(
			callerRoles,
			(roles ?? []).map((r) => r.name ?? '')
		).filter((role) => !targetRoles.includes(role))
	);

	/** Returns true if the caller can remove a specific role from the target. */
	function canRemoveRole(role: string): boolean {
		return canManage && getRoleRank(role) < callerRank;
	}

	let selectedRole = $state('');

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
		} else {
			toast.error(getErrorMessage(error, m.admin_userDetail_roleRemoveError()));
		}
	}

	async function lockUser() {
		isLocking = true;
		const { response, error } = await browserClient.POST('/api/v1/admin/users/{id}/lock', {
			params: { path: { id: user.id ?? '' } }
		});
		isLocking = false;

		if (response.ok) {
			toast.success(m.admin_userDetail_lockSuccess());
			await invalidateAll();
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
		} else {
			toast.error(getErrorMessage(error, m.admin_userDetail_deleteError()));
		}
	}
</script>

<div class="grid gap-6">
	<!-- Account Information Card -->
	<Card.Root>
		<Card.Header>
			<Card.Title>{m.admin_userDetail_accountInfo()}</Card.Title>
			<Card.Description>{m.admin_userDetail_accountInfoDescription()}</Card.Description>
		</Card.Header>
		<Card.Content>
			<div class="grid gap-4 sm:grid-cols-2">
				<div class="flex items-start gap-3">
					<Hash class="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
					<div class="min-w-0">
						<p class="text-xs font-medium text-muted-foreground">{m.admin_userDetail_userId()}</p>
						<p class="truncate text-sm">{user.id}</p>
					</div>
				</div>
				<div class="flex items-start gap-3">
					<UserIcon class="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
					<div class="min-w-0">
						<p class="text-xs font-medium text-muted-foreground">
							{m.admin_userDetail_username()}
						</p>
						<p class="truncate text-sm">{user.username}</p>
					</div>
				</div>
				<div class="flex items-start gap-3">
					<Mail class="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
					<div class="min-w-0">
						<p class="text-xs font-medium text-muted-foreground">{m.admin_userDetail_email()}</p>
						<p class="truncate text-sm">{user.email}</p>
					</div>
				</div>
				<div class="flex items-start gap-3">
					<UserIcon class="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
					<div class="min-w-0">
						<p class="text-xs font-medium text-muted-foreground">
							{m.admin_userDetail_firstName()}
						</p>
						<p class="text-sm">{user.firstName ?? m.admin_userDetail_notSet()}</p>
					</div>
				</div>
				<div class="flex items-start gap-3">
					<UserIcon class="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
					<div class="min-w-0">
						<p class="text-xs font-medium text-muted-foreground">
							{m.admin_userDetail_lastName()}
						</p>
						<p class="text-sm">{user.lastName ?? m.admin_userDetail_notSet()}</p>
					</div>
				</div>
				<div class="flex items-start gap-3">
					<Phone class="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
					<div class="min-w-0">
						<p class="text-xs font-medium text-muted-foreground">{m.admin_userDetail_phone()}</p>
						<p class="text-sm">{user.phoneNumber ?? m.admin_userDetail_notSet()}</p>
					</div>
				</div>
				<div class="flex items-start gap-3">
					{#if user.emailConfirmed}
						<CheckCircle class="mt-0.5 h-4 w-4 shrink-0 text-green-500" />
					{:else}
						<XCircle class="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
					{/if}
					<div class="min-w-0">
						<p class="text-xs font-medium text-muted-foreground">
							{m.admin_userDetail_emailConfirmed()}
						</p>
						<p class="text-sm">
							{user.emailConfirmed ? m.admin_userDetail_yes() : m.admin_userDetail_no()}
						</p>
					</div>
				</div>
				<div class="flex items-start gap-3">
					<Shield class="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" />
					<div class="min-w-0">
						<p class="text-xs font-medium text-muted-foreground">
							{m.admin_userDetail_accessFailedCount()}
						</p>
						<p class="text-sm">{user.accessFailedCount ?? 0}</p>
					</div>
				</div>
			</div>
		</Card.Content>
	</Card.Root>

	<!-- Role Management Card -->
	<Card.Root>
		<Card.Header>
			<Card.Title>{m.admin_userDetail_roleManagement()}</Card.Title>
			<Card.Description>{m.admin_userDetail_roleManagementDescription()}</Card.Description>
		</Card.Header>
		<Card.Content class="space-y-4">
			<div>
				<p class="mb-2 text-sm font-medium">{m.admin_userDetail_currentRoles()}</p>
				<div class="flex flex-wrap gap-2">
					{#each user.roles ?? [] as role (role)}
						<Badge variant="secondary" class="gap-1">
							{role}
							{#if canRemoveRole(role)}
								<button
									class="ms-1 inline-flex h-4 w-4 items-center justify-center rounded-full hover:bg-muted"
									aria-label="{m.admin_userDetail_removeRole()} {role}"
									disabled={isRemovingRole === role}
									onclick={() => removeRole(role)}
								>
									<X class="h-3 w-3" />
								</button>
							{/if}
						</Badge>
					{:else}
						<span class="text-sm text-muted-foreground">{m.admin_userDetail_noRoles()}</span>
					{/each}
				</div>
			</div>

			{#if canManage && availableRoles.length > 0}
				<div class="flex items-end gap-2">
					<div class="flex-1">
						<label for="role-select" class="mb-1 block text-sm font-medium">
							{m.admin_userDetail_assignRole()}
						</label>
						<select
							id="role-select"
							bind:value={selectedRole}
							class="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:outline-none"
						>
							<option value="">---</option>
							{#each availableRoles as role (role)}
								<option value={role}>{role}</option>
							{/each}
						</select>
					</div>
					<Button size="sm" disabled={!selectedRole || isAssigningRole} onclick={assignRole}>
						<Plus class="me-1 h-4 w-4" />
						{m.admin_userDetail_assignRole()}
					</Button>
				</div>
			{/if}
		</Card.Content>
	</Card.Root>

	<!-- Account Actions Card -->
	<Card.Root>
		<Card.Header>
			<Card.Title>{m.admin_userDetail_accountActions()}</Card.Title>
			<Card.Description>{m.admin_userDetail_accountActionsDescription()}</Card.Description>
		</Card.Header>
		<Card.Content>
			{#if canManage}
				<div class="flex flex-wrap gap-3">
					{#if user.isLockedOut}
						<Button variant="outline" disabled={isUnlocking} onclick={unlockUser}>
							<Unlock class="me-2 h-4 w-4" />
							{m.admin_userDetail_unlockAccount()}
						</Button>
					{:else}
						<Button variant="outline" disabled={isLocking} onclick={lockUser}>
							<Lock class="me-2 h-4 w-4" />
							{m.admin_userDetail_lockAccount()}
						</Button>
					{/if}

					<Dialog.Root bind:open={deleteDialogOpen}>
						<Dialog.Trigger>
							{#snippet child({ props })}
								<Button variant="destructive" {...props}>
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
									{m.admin_userDetail_deleteCancel()}
								</Button>
								<Button variant="destructive" disabled={isDeleting} onclick={deleteUser}>
									{m.admin_userDetail_deleteConfirm()}
								</Button>
							</Dialog.Footer>
						</Dialog.Content>
					</Dialog.Root>
				</div>
			{:else}
				<p class="text-sm text-muted-foreground">
					{m.admin_userDetail_cannotManage()}
				</p>
			{/if}
		</Card.Content>
	</Card.Root>
</div>
