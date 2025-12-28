import { type ClassValue, clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]): string {
	return twMerge(clsx(inputs));
}

export type WithElementRef<T, R = HTMLElement> = T & {
	ref?: R | null;
};

export type WithoutChildrenOrChild<T> = T extends { children?: unknown; child?: unknown }
	? Omit<T, 'children' | 'child'>
	: T;

export type WithoutChild<T> = T extends { child?: unknown } ? Omit<T, 'child'> : T;
