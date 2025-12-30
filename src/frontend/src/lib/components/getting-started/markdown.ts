/**
 * Simple markdown renderer for displaying README content.
 * Handles common markdown patterns - not a full parser, but sufficient for project READMEs.
 */
export function renderMarkdown(md: string): string {
	return (
		md
			// Escape HTML first
			.replace(/&/g, '&amp;')
			.replace(/</g, '&lt;')
			.replace(/>/g, '&gt;')
			// Code blocks (```lang ... ```)
			.replace(/```(\w*)\n([\s\S]*?)```/g, (_m, _lang, code) => {
				return `<pre class="rounded-lg bg-muted p-4 overflow-x-auto"><code class="text-sm text-foreground">${code.trim()}</code></pre>`;
			})
			// Inline code
			.replace(/`([^`]+)`/g, '<code class="rounded bg-muted px-1.5 py-0.5 text-sm">$1</code>')
			// Headers
			.replace(/^### (.+)$/gm, '<h3 class="text-lg font-semibold mt-6 mb-2">$1</h3>')
			.replace(/^## (.+)$/gm, '<h2 class="text-xl font-semibold mt-8 mb-3">$1</h2>')
			.replace(/^# (.+)$/gm, '<h1 class="text-2xl font-bold mt-4 mb-4">$1</h1>')
			// Bold and italic
			.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
			.replace(/\*([^*]+)\*/g, '<em>$1</em>')
			// Links
			.replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2" class="text-primary underline">$1</a>')
			// Tables - process complete table blocks
			.replace(/(\|.+\|\n)+/g, (tableBlock) => {
				const rows = tableBlock.trim().split('\n');
				if (rows.length < 2) return tableBlock;

				const parseRow = (row: string) =>
					row
						.split('|')
						.filter((c) => c.trim())
						.map((c) => c.trim());

				const headerCells = parseRow(rows[0]);
				const headerHtml = `<thead><tr>${headerCells.map((c) => `<th class="border border-border bg-muted/50 px-3 py-2 text-start font-semibold">${c}</th>`).join('')}</tr></thead>`;

				// Skip separator row (row 1), process data rows (row 2+)
				const bodyRows = rows.slice(2);
				const bodyHtml =
					bodyRows.length > 0
						? `<tbody>${bodyRows
								.map(
									(row) =>
										`<tr>${parseRow(row)
											.map((c) => `<td class="border border-border px-3 py-2">${c}</td>`)
											.join('')}</tr>`
								)
								.join('')}</tbody>`
						: '';

				return `<table class="w-full border-collapse my-4">${headerHtml}${bodyHtml}</table>`;
			})
			// Unordered lists
			.replace(/^- (.+)$/gm, '<li class="ms-4">$1</li>')
			.replace(/(<li[\s\S]*?<\/li>\n?)+/g, '<ul class="list-disc my-2">$&</ul>')
			// Numbered lists
			.replace(/^\d+\. (.+)$/gm, '<li class="ms-4">$1</li>')
			// Paragraphs (double newlines)
			.replace(/\n\n/g, '</p><p class="my-3">')
			// Single newlines in regular text
			.replace(/\n/g, '<br>')
			// Wrap in paragraph
			.replace(/^/, '<p class="my-3">')
			.replace(/$/, '</p>')
			// Clean up empty paragraphs
			.replace(/<p class="my-3"><\/p>/g, '')
			.replace(/<p class="my-3">(<h[123])/g, '$1')
			.replace(/(<\/h[123]>)<\/p>/g, '$1')
			.replace(/<p class="my-3">(<pre)/g, '$1')
			.replace(/(<\/pre>)<\/p>/g, '$1')
			.replace(/<p class="my-3">(<table)/g, '$1')
			.replace(/(<\/table>)<\/p>/g, '$1')
			.replace(/<p class="my-3">(<ul)/g, '$1')
			.replace(/(<\/ul>)<\/p>/g, '$1')
	);
}
