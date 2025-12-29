<script lang="ts">
	import { LoginForm } from '$lib/components/auth';
	import * as m from '$lib/paraglide/messages';
	let { data } = $props();
</script>

<svelte:head>
	<title>{m.meta_titleTemplate({ title: m.meta_login_title() })}</title>
	<meta name="description" content={m.meta_login_description()} />
</svelte:head>

<LoginForm apiUrl={data.apiUrl} />
