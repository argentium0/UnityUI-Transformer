---
description: Enforces strict adherence to the project's foundational architecture documents.
globs: *
---
# Strict Architecture & Build Plan Adherence

1. **Source of Truth:** You must strictly follow `docs/01-tech-stack.md` and `docs/02-prd.md`. Do not suggest or implement alternative frameworks (e.g., React instead of Preact, or uGUI over UI Toolkit for primary generation) unless explicitly requested.
2. **Phase Adherence:** When executing a phase from `03-antigravity-build-plan.md`, do not skip ahead. Only implement the scope defined for that specific phase.
3. **Verification:** Always propose a clear implementation plan before executing multi-file writes.
