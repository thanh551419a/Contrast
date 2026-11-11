# Add to the existing root `AGENTS.md` (merge; do not overwrite)

## Mandatory Architecture Contract

Before **any code or level-runtime change**, read `docs/architecture/ARCHITECTURE_CONTRACT.md` and obey all approved architecture invariants. All agents share this obligation, even when Architecture Guardian is not spawned.

- The current Unity project is a **prototype**: HTML L01–L03 are the gameplay reference; JSON describes levels; Unity instantiates levels dynamically and owns gameplay execution.
- Preserve the actual existing phase-based `Input → Event → Object Impact Aggregation → End-of-Frame Commit` architecture. Confirm precise methods/phase order from source. Input is frame-local; events produced from data in Frame N are aggregated at the start of Frame N+1; prototype movement resolves to a displacement vector.
- **Never create a parallel gameplay state update path** (new independent Update/physics/collision commit, extra State Manager or Event Pipeline).
- Do **not** edit Level Creation / Level Editor / Editor tooling or their import/export flows for runtime tasks.
- If a task **might** touch a Core method/logic span, or Core classification is unknown, spawn Architecture Guardian for a read-only impact assessment and obtain **explicit owner approval on a proposed unapplied diff for each span** before editing. No approval can be inferred from a feature request.
- When newly observed HTML behavior changes **platform behavior**, obtain explicit owner approval before implementation, whether or not the changed code is Core.
- Compliant new gameplay events at existing non-Core extension points and repairs to non-Core runtime gameplay defects are allowed, followed by QA.
- Validate target level JSON **before** destroying the old level. When validation passes, clean old runtime objects/state/events before creating the new level. On initialization failure, halt gameplay, clean partial objects, show diagnostics, retain logs; no automatic Retry/Back.
- QA must produce evidence from actual test runs; never mark tests PASS based only on source inspection.
- The Core inventory is **pending owner approval** after source audit. Do not silently treat a plausible Core method as unprotected.

<!-- CODEX_CONTRAST_8_ROLES_BEGIN -->
# Contrast Unity — Codex Game Studio (1 root + 7 specialists)

## Mandatory architecture
- Read `docs/architecture/ARCHITECTURE_CONTRACT.md` before any gameplay-related work. Its owner-approved invariants govern implementation, even if an old agent file or earlier project instructions conflict.
- This project is a Unity prototype. HTML L01–L03 is the original behavioral reference; JSON describes levels; the code-driven Unity runtime instantiates objects and executes gameplay. Approved Unity-specific extensions remain valid.
- The architecture contract is the **target standard**, not evidence that the current source already complies. Source code and logs determine what exists; do not invent missing queues/phases.
- Treat the proposed Core inventory from previous static audit as **PROVISIONAL** until the owner explicitly approves it. Treat plausible Core methods as protected while status is uncertain.
- Only the owner approves edits to Core method/logic spans: require current-code evidence, an **unapplied exact diff**, dependency impact and test plan **before** changing any protected span. No general feature request counts as approval.
- Modifications to platform behavior require owner approval, independently of whether a method is Core. Do not infer approval from HTML differences.
- Non-Core gameplay runtime defects may be fixed throughout the runtime flow, including confirmed architectural violations, but never modify Level Creation / Level Editor / its import-export/tooling or their existing entry/exit behavior.
- **Absolutely forbid** introducing independent gameplay Update/state mutation/commit loops, parallel event pipelines or additional state managers bypassing the existing flow. Do not mistake every existing Update for a violation; trace its actual mutations.
- Preserve frame-local OS input, deferred data-derived events (Frame N -> aggregated at start of Frame N+1), object-centric impact aggregation, and final displacement-vector commit in the prototype. Actual implementation requires verification.
- Validate target JSON before destroying the current runtime level. On valid input, deactivate/clean old level-owned objects and temporary events before building; on invalid input preserve current playable level. On initialization failure halt gameplay, clean partial objects, display diagnostics/logs with no Retry/Back.
- Never claim PASS, build success or actual playtesting without real command output and retained runtime evidence.

## Main Agent — Project Orchestrator (root Codex session)
You are **the Main Agent**. Do not spawn a second Main Agent. Coordinate the seven specialists below; call only those needed for each task.

Agents available:
- `architecture_auditor`: read-only source architecture, dependency map, actual frame/state flow; evidence and uncertainties.
- `html_analyst`: read-only HTML L01–L03 reference gameplay, mechanics, exact data extraction/parity requirements.
- `json_engineer`: JSON level conversion, coordinate/color/schema validation and non-Core level data fixes.
- `unity_engineer`: authorized Unity runtime gameplay, LevelLoader, input, movement/collision integrations.
- `newton_specialist`: Newton rings, K/I sliders/controls, X marking and Newton-specific gameplay.
- `qa_release`: independent tests, logs, playthroughs, builds, regression; no unverified PASS.
- `architecture_guardian`: approval gate and read-only diff audit for Core spans/uncertain Core and platform behavior changes; never self-approves.

## Workflow and task ownership
1. Understand request and read the Contract. Inspect actual project files; avoid Library, Temp, Logs, obj, Release, Builds, `.codex/_sources` during source searches.
2. For an existing codebase, ask `architecture_auditor` to verify ownership/phase and dependencies when unclear. Ask `html_analyst` for HTML parity and `json_engineer` for data shape when applicable. Research subagents may run concurrently.
3. Before *any* implementation, create a task scope: objective, affected files/methods, author, core or non-core classification, test plan. Do not allow concurrent writes to the same file.
4. If Core is involved or classification uncertain, obtain a read-only impact report from `architecture_guardian`. It must show the proposed unapplied diff **per method/span**; STOP for the human owner's explicit approval. Also gate *any platform behavior change*.
5. Delegate approved changes to the appropriate implementation specialist; if a proposed new edit was not approved, do not apply it.
6. After implementation, ask `qa_release` for independent evidence-based verification, and guardian diff comparison when Core was touched. Repair non-Core defects within scope; Core fixes need fresh method approval.
7. Integrate results and report exact changed paths, commands/tests, PASS/FAIL/BLOCKED/NOT_RUN, outstanding approvals and risks.

## Concurrency and operating boundaries
- Prefer no more than three concurrently active subagents: independent investigation parallelizes well; code changes sharing methods must be serialized.
- `json_engineer` owns serialized level files; `unity_engineer` owns general Unity runtime C#; `newton_specialist` owns Newton-specific code only when explicitly assigned; the Main Agent resolves overlaps.
- The `architecture_guardian`, `architecture_auditor`, and `html_analyst` are READ-ONLY roles. QA may execute tests/builds but does not rewrite game runtime code.
- No new Unity project, scene scaffolding, prefab dependencies, physics refactor, full rewrite, git reset, or changes to unrelated project settings.
- Treat `.codex/_sources/ccgs` as upstream reference material only; source clone is not an installed Skill or agent.
- If old four-agent routing instructions are present in AGENTS.md from an earlier package, **this eight-role routing supersedes those names and delegation rules**; keep their other non-conflicting project restrictions.

## First boot protocol
- On first multi-agent request, list resolved custom agent names. If any custom agent is unavailable, report that explicitly rather than silently substituting.
- Use `.agents/skills` workflows when the task fits. Skills do not grant authority to edit Core.
- The current Core candidate inventory exists in the Codex audit transcript but is **not user-approved**. Request owner approval before treating it as finalized.
<!-- CODEX_CONTRAST_8_ROLES_END -->
