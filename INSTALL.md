# Manual integration — keep all existing configuration

1. Copy `docs/architecture/ARCHITECTURE_CONTRACT.md` into the same relative path of your Unity project root.
2. Open your **existing** root `AGENTS.md` and merge the text in `AGENTS_REFERENCE_SNIPPET.md`; do not overwrite existing instructions.
3. Start Codex CLI at the Unity project root and paste `FIRST_GUARDIAN_AUDIT_PROMPT.md` to perform the first audit.
4. Only after approving the core inventory should the owner authorize implementation of a dedicated Architecture Guardian agent configuration. This bundle intentionally does **not** install a runnable agent or edit Unity source.

For other repository layouts, adjust the path in AGENTS.md consistently.
