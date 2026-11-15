# Project History

## Current state

The repository no longer vendors the `design-taste-frontend` skill. Product visual guidance remains in `docs/PRODUCT_REQUIREMENTS.md` without requiring that skill.

## Operations

### 2026-11-15 — Remove the tast skill from the application repo

- Removed `.agents/skills/design-taste-frontend/` and its entry from `skills-lock.json`.
- Removed the skill-specific usage instructions from `docs/PRODUCT_REQUIREMENTS.md`; retained its visual design direction as ordinary product guidance.
- Updated `AGENTS.md` with the current handoff.
- The initial sandboxed directory removal was denied by file permissions; the requested removal completed after filesystem escalation.
- Verification: searched for active skill references (only this history and the handoff mention the removed skill), reviewed Git status, and ran `git diff --check` successfully. No tests were run because this change only removes documentation and skill assets.
- Commit/push: not performed.
- Remaining work: none.
