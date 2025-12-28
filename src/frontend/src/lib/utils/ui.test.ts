import { describe, it, expect } from 'vitest';
import { cn } from '$lib/utils';

describe('utils', () => {
	describe('cn', () => {
		it('merges class names correctly', () => {
			expect(cn('c-1', 'c-2')).toBe('c-1 c-2');
		});

		it('handles conditional classes', () => {
			const isTrue = true;
			const isFalse = false;
			expect(cn('c-1', isTrue && 'c-2', isFalse && 'c-3')).toBe('c-1 c-2');
		});

		it('merges tailwind classes using tailwind-merge', () => {
			expect(cn('px-2 py-1', 'p-4')).toBe('p-4');
		});
	});
});
