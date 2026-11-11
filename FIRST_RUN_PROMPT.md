# First verification request for Codex CLI (paste within Codex)

You are the Main Project Orchestrator in `Y:\Contrast\My project`.
First read `AGENTS.md`, `docs/architecture/ARCHITECTURE_CONTRACT.md`, `.codex/config.toml` and list available custom agents and `.agents/skills`.

Run an agent-discovery smoke test, **without editing any source**:
- Ask `architecture_auditor` to verify the actual runtime frame pipeline and candidate Core boundaries from source evidence.
- Ask `html_analyst` to summarize the L01-L03 HTML behavior reference and identify differences that require approval.
- Ask `architecture_guardian` to inspect the previous Core inventory proposal, report its approval state and explain the per-method change gate.
- Ask `qa_release` to propose an evidence-driven smoke/regression plan without running destructive commands.

Run up to three subagents concurrently; wait for their results before summarizing. Do not spawn an implementation agent for code changes yet.
Report which named agents and Skill names are actually discovered by this Codex session, any errors, exact source evidence, and items needing owner approval.
Do not modify Unity source, scenes, JSON, Editor, config or architecture docs.
