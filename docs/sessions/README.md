# Session Documentation

This directory contains documentation generated at the end of each AI-assisted coding session.

## File Naming

```
{YYYY-MM-DD}-{topic-slug}.md
```

Examples:
- `2026-02-07-add-orders-feature.md`
- `2026-02-08-fix-auth-refresh-race-condition.md`
- `2026-02-10-refactor-caching-layer.md`

## Required Template

Every session doc must follow this structure:

```markdown
# {Session Title}

**Date**: {YYYY-MM-DD}
**Scope**: {Brief description of what was worked on}

## Summary

{2-3 sentence overview of what was accomplished}

## Changes Made

| File | Change | Reason |
|------|--------|--------|
| `path/to/file` | {What changed} | {Why} |

## Decisions & Reasoning

### {Decision 1 Title}

- **Choice**: {What was decided}
- **Alternatives considered**: {Other options}
- **Reasoning**: {Why this option was chosen}

## Diagrams

{Include Mermaid diagrams where they add clarity — entity relationships,
request flows, state transitions, service maps.

Use the appropriate diagram type:
- flowchart TD — request/data flows
- erDiagram — entity relationships
- sequenceDiagram — multi-step interactions
- classDiagram — interface/implementation maps
- stateDiagram-v2 — state transitions

Skip this section entirely if the work was trivial and diagrams wouldn't add value.}

## Follow-Up Items

- [ ] {Any remaining work or known issues}
```

## Commit

Session docs are committed as the **final commit** of the session:

```
docs: add session notes for {topic}
```
