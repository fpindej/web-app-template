import type { RequestHandler } from './$types';
import { SERVER_CONFIG } from '$lib/server/config';

export const fallback: RequestHandler = async ({ request, params, url, fetch }) => {
	const targetUrl = `${SERVER_CONFIG.API_URL}/api/${params.path}${url.search}`;

	const newRequest = new Request(targetUrl, {
		method: request.method,
		headers: request.headers,
		body: request.body,
		// @ts-expect-error - duplex is needed for streaming bodies in some node versions/fetch implementations
		duplex: 'half'
	});

	newRequest.headers.delete('host');
	newRequest.headers.delete('connection');
	// Note: We are forwarding all other headers (including Cookie and Authorization)
	// This is necessary for auth to work, but be aware of the security implications
	// if the backend is not trusted or if this proxy is exposed to untrusted clients.

	try {
		const response = await fetch(newRequest);
		return response;
	} catch (err) {
		console.error('Proxy error:', err);

		// Check if it's a connection error (e.g. backend down)
		// @ts-expect-error - cause is not typed in standard Error but exists in fetch errors
		if (err?.cause?.code === 'ECONNREFUSED') {
			return new Response(JSON.stringify({ message: 'Backend unavailable' }), {
				status: 503,
				headers: { 'Content-Type': 'application/json' }
			});
		}

		return new Response('Bad Gateway', { status: 502 });
	}
};
