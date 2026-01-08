import { env } from '$env/dynamic/private';

export const SERVER_CONFIG = {
	API_URL: env.API_URL || 'http://localhost:{INIT_API_PORT}'
};
