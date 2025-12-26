<script lang="ts">
	import { LoginForm } from '$lib/components/auth';
	let { data } = $props();
</script>

<LoginForm apiUrl={data.apiUrl} />
