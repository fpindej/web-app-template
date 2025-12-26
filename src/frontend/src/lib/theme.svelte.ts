import { browser } from '$app/environment';

type Theme = 'light' | 'dark' | 'system';

let theme = $state<Theme>('system');

export function getTheme() {
	return theme;
}

export function setTheme(newTheme: Theme) {
	theme = newTheme;
	if (browser) {
		try {
			localStorage.setItem('theme', newTheme);
		} catch {
			// Ignore write errors
		}
		applyTheme(newTheme);
	}
}

export function toggleTheme() {
	const current = getTheme();
	if (current === 'light') {
		setTheme('dark');
	} else if (current === 'dark') {
		setTheme('light');
	} else {
		// system
		if (window.matchMedia('(prefers-color-scheme: dark)').matches) {
			setTheme('light');
		} else {
			setTheme('dark');
		}
	}
}

function applyTheme(t: Theme) {
	if (!browser) return;

	const root = document.documentElement;
	const isDark =
		t === 'dark' || (t === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches);

	if (isDark) {
		root.classList.add('dark');
	} else {
		root.classList.remove('dark');
	}
}

export function initTheme() {
	if (!browser) return;

	try {
		const savedTheme = localStorage.getItem('theme') as Theme | null;
		if (savedTheme) {
			theme = savedTheme;
		}
	} catch {
		theme = 'system';
	}
	applyTheme(theme);

	const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
	const handleChange = () => {
		if (theme === 'system') {
			applyTheme('system');
		}
	};

	mediaQuery.addEventListener('change', handleChange);

	return () => {
		mediaQuery.removeEventListener('change', handleChange);
	};
}
