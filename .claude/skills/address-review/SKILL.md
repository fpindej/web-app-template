---
description: Read PR review comments from GitHub, evaluate, address, and reply
user-invocable: true
argument-hint: '[PR number]'
---

# /address-review

Read review comments on a GitHub PR, evaluate each one, and address it appropriately.

## Steps

1. **Resolve the PR number.** If an argument is provided, use it. Otherwise, detect from current branch:

```bash
gh pr view --json number,title,url
```

2. **Fetch all review comments** (includes inline code comments and review threads):

```bash
gh api --method GET repos/{owner}/{repo}/pulls/{number}/comments
gh api --method GET repos/{owner}/{repo}/pulls/{number}/reviews
```

(Always pass `--method GET` explicitly for `gh api` reads - only explicit-GET calls are pre-approved by the project permissions.)

Filter to only unresolved threads with actual review content. Read the full thread (all replies in each discussion), not just the first comment.

3. **For each unresolved thread, evaluate what type of comment it is:**

### A. Change request ("change this", "use X instead of Y", "add Z")

1. Read the referenced file + surrounding context
2. Evaluate if the change is correct, factual, and worth it
3. If yes: make the change, reply with what was changed and which commit
4. If the change would break something or is incorrect: explain why and propose an alternative

### B. Question ("why is this here?", "doesn't X already handle...?")

Reply with a clear explanation. Reference specific code/docs. If the answer reveals a real issue, fix it too.

### C. Suggestion / discussion ("we could...", "what if we...")

Evaluate effort vs impact. Reply with one of:

- "Good idea, I'll do it in this PR" (and do it)
- "Makes sense, but better as a follow-up" (explain why)
- "I don't think it's needed because..." (explain reasoning)

### D. Factual correction ("that's not true", "table doesn't have column X")

Verify the claim by reading the actual code/schema. If the reviewer is right, fix it. If wrong, explain with evidence.

4. **Reply to each thread:**

```bash
gh api repos/{owner}/{repo}/pulls/{number}/comments/{comment_id}/replies --method POST -f "body=<reply>"
```

Do NOT resolve threads that need further discussion from the reviewer.

5. **After addressing all comments**, run `/verify`.

6. **Commit and push** with a descriptive message like `fix: address review - <summary of changes>`.

## Rules

- Read the FULL thread before responding (earlier replies give context)
- Reference specific commits when you make a change
- If a change requires a migration, new endpoint, or significant scope: discuss before implementing
- Never resolve a thread without either fixing the code or explaining why no change is needed
- If review comments contradict each other, flag the conflict for the user instead of trying to satisfy each of them
- Group related fixes into a single commit when they address the same concern
- Run `/verify` before pushing
- No em dashes in replies
