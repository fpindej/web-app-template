<script lang="ts">
	import * as Dialog from '$lib/components/ui/dialog';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import { Label } from '$lib/components/ui/label';
	import { PhoneInput } from '$lib/components/ui/phone-input';
	import { browserClient } from '$lib/api';
	import * as m from '$lib/paraglide/messages';
	import { toast } from '$lib/components/ui/sonner';
	import { Loader2 } from '@lucide/svelte';
	import { isValidationProblemDetails, mapFieldErrors } from '$lib/api';
	import { createFieldShakes } from '$lib/state';

	let { open = $bindable(false), onSuccess } = $props<{
		open?: boolean;
		onSuccess?: (email: string) => void;
	}>();

	let email = $state('');
	let password = $state('');
	let confirmPassword = $state('');
	let firstName = $state('');
	let lastName = $state('');
	let phoneNumber = $state('');
	let isLoading = $state(false);
	let error = $state<string | null>(null);
	let fieldErrors = $state<Record<string, string>>({});
	const fieldShakes = createFieldShakes();

	function resetForm() {
		email = '';
		password = '';
		confirmPassword = '';
		firstName = '';
		lastName = '';
		phoneNumber = '';
		error = null;
		fieldErrors = {};
	}

	function handleOpenChange(isOpen: boolean) {
		if (!isOpen) {
			resetForm();
		}
	}

	async function register(e: Event) {
		e.preventDefault();
		isLoading = true;
		error = null;
		fieldErrors = {};

		if (password !== confirmPassword) {
			fieldErrors = { confirmPassword: m.auth_register_passwordMismatch() };
			fieldShakes.trigger('confirmPassword');
			isLoading = false;
			return;
		}

		try {
			const { response, error: apiError } = await browserClient.POST('/api/auth/register', {
				body: {
					email,
					password,
					firstName: firstName || undefined,
					lastName: lastName || undefined,
					phoneNumber: phoneNumber || undefined
				}
			});

			if (response.ok) {
				toast.success(m.auth_register_success());
				const registeredEmail = email;
				open = false;
				onSuccess?.(registeredEmail);
			} else if (apiError) {
				// Extract validation errors from the errors object if present
				if (isValidationProblemDetails(apiError)) {
					fieldErrors = mapFieldErrors(apiError.errors);
					fieldShakes.triggerFields(Object.keys(fieldErrors));
				} else {
					error = apiError.detail || apiError.title || m.auth_register_failed();
				}
			} else {
				error = m.auth_register_failed();
			}
		} catch {
			error = m.auth_register_failed();
		} finally {
			isLoading = false;
		}
	}
</script>

<Dialog.Root bind:open onOpenChange={handleOpenChange}>
	<Dialog.Content class="sm:max-w-[425px]">
		<Dialog.Header>
			<Dialog.Title>{m.auth_register_title()}</Dialog.Title>
			<Dialog.Description>
				{m.auth_register_description()}
			</Dialog.Description>
		</Dialog.Header>
		<form
			onsubmit={register}
			class="grid gap-4 py-4"
			aria-describedby={error ? 'register-error' : undefined}
		>
			{#if error}
				<div id="register-error" role="alert" class="text-sm font-medium text-destructive">
					{error}
				</div>
			{/if}
			<div class="grid grid-cols-2 gap-4">
				<div class="grid gap-2">
					<Label for="firstName">{m.auth_register_firstName()}</Label>
					<Input
						id="firstName"
						autocomplete="given-name"
						bind:value={firstName}
						disabled={isLoading}
						class={fieldShakes.class('firstName')}
						aria-invalid={!!fieldErrors.firstName}
					/>
					{#if fieldErrors.firstName}
						<p class="text-xs text-destructive">{fieldErrors.firstName}</p>
					{/if}
				</div>
				<div class="grid gap-2">
					<Label for="lastName">{m.auth_register_lastName()}</Label>
					<Input
						id="lastName"
						autocomplete="family-name"
						bind:value={lastName}
						disabled={isLoading}
						class={fieldShakes.class('lastName')}
						aria-invalid={!!fieldErrors.lastName}
					/>
					{#if fieldErrors.lastName}
						<p class="text-xs text-destructive">{fieldErrors.lastName}</p>
					{/if}
				</div>
			</div>
			<div class="grid gap-2">
				<Label for="email">{m.auth_register_email()}</Label>
				<Input
					id="email"
					type="email"
					autocomplete="email"
					bind:value={email}
					required
					disabled={isLoading}
					class={fieldShakes.class('email')}
					aria-invalid={!!fieldErrors.email}
				/>
				{#if fieldErrors.email}
					<p class="text-xs text-destructive">{fieldErrors.email}</p>
				{/if}
			</div>
			<div class="grid gap-2">
				<Label for="phone">{m.auth_register_phone()}</Label>
				<PhoneInput
					id="phone"
					bind:value={phoneNumber}
					disabled={isLoading}
					class={fieldShakes.class('phoneNumber')}
					aria-invalid={!!fieldErrors.phoneNumber}
				/>
				{#if fieldErrors.phoneNumber}
					<p class="text-xs text-destructive">{fieldErrors.phoneNumber}</p>
				{/if}
			</div>
			<div class="grid gap-2">
				<Label for="password">{m.auth_register_password()}</Label>
				<Input
					id="password"
					type="password"
					autocomplete="new-password"
					bind:value={password}
					required
					minlength={6}
					disabled={isLoading}
					class={fieldShakes.class('password')}
					aria-invalid={!!fieldErrors.password}
				/>
				{#if fieldErrors.password}
					<p class="text-xs text-destructive">{fieldErrors.password}</p>
				{/if}
			</div>
			<div class="grid gap-2">
				<Label for="confirmPassword">{m.auth_register_confirmPassword()}</Label>
				<Input
					id="confirmPassword"
					type="password"
					autocomplete="new-password"
					bind:value={confirmPassword}
					required
					minlength={6}
					disabled={isLoading}
					class={fieldShakes.class('confirmPassword')}
					aria-invalid={!!fieldErrors.confirmPassword}
				/>
				{#if fieldErrors.confirmPassword}
					<p class="text-xs text-destructive">{fieldErrors.confirmPassword}</p>
				{/if}
			</div>
			<Dialog.Footer>
				<Button type="submit" disabled={isLoading} class="w-full">
					{#if isLoading}
						<Loader2 class="me-2 h-4 w-4 animate-spin" />
					{/if}
					{m.auth_register_submit()}
				</Button>
			</Dialog.Footer>
		</form>
	</Dialog.Content>
</Dialog.Root>
