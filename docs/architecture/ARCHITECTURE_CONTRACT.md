# ARCHITECTURE CONTRACT — Unity HTML L01–L03 / JSON / Newton

**Version:** 2.8 — consolidated decisions through AC-027  
**Status:** User-approved principles; source-level architecture inventory is **PENDING AUDIT**.  
**Authority:** Project owner.  
**Scope:** Unity prototype gameplay runtime reproducing HTML L01–L03, JSON-described levels, Newton-ring gameplay.  
**Not a source audit:** Class names, method names, file paths, actual phase order and implementations must be verified from the current repository.

> This document defines accepted design constraints. It does not certify that the existing code complies with them. It does not authorize edits to protected core methods, the Level Editor, or platform behavior.

## 1. Source of truth and data boundaries

### AC-002 — HTML gameplay authority
- HTML L01–L03 are the behavioral reference for original gameplay.
- Unity reproduces the corresponding gameplay and may contain **approved Unity-specific mechanisms** absent from HTML.
- A difference between Unity and HTML is not automatically a bug. Check whether it is an intentional extension.
- The existing overlap-threshold collision mechanism (example discussed: 50% overlap) is **already implemented**; the example does not authorize rewriting it.

### AC-003 — JSON is a level description, not gameplay code
- HTML level data is transcribed/converted into JSON; Unity reads the same supported schema and generates a visually and behaviorally equivalent level using its existing generic runtime.
- Coordinates, size, color, checkpoints, platforms, trajectories and other supported level properties belong in the level data where applicable.
- Adding a new level that uses existing supported types must not require writing C# for that level.
- JSON does not implement gameplay algorithms. HTML provides reference behavior; the Unity runtime implements behavior.
- Do not hardcode per-level objects or coordinates in gameplay code; do not create an L01/L02/L03-specific loader.
- Support for a **new object/behavior type** may require new general runtime behavior; its approval is governed by AC-004, AC-008 and AC-019.

### AC-004 — Approval when platform behavior differs
- When newly observed HTML behavior changes how a **platform** acts, prepare an impact report and obtain the owner's approval **before modifying code**.
- Other new gameplay mechanisms may be implemented without a separate feature-level approval if they preserve this Contract, do not touch protected core methods, and do not alter platform behavior.
- If a requested change conflicts with an accepted invariant, stop and escalate to the owner.

## 2. Game runtime and frame pipeline

### AC-005/006/007 — Reuse the existing pipeline; verify its implementation
- The project is understood to already have a pipeline conceptually described as **Input → Event → Impact Aggregation → End-of-Frame Commit**.
- Read actual source to identify its classes, methods, event storage, state ownership, and specific execution order; never invent a second pipeline.
- Trace other runtime paths (collision, trajectory, checkpoint, Newton, animation) and report observed exceptions **without automatically redefining them**.
- The owner will approve the detailed core-system inventory and uncertain execution rules after the audit.

### AC-009 — Phase-based execution
- Similar operations must execute in the same designated phase; do not scatter equivalent processing or split the game into independent per-feature update-and-commit loops.
- Preserve existing phases and ordering; do not redesign based merely on a conceptual diagram.

### AC-010 — Deferred data-derived events
- **Data-derived events generated in Frame N** are aggregated for their impacts at the beginning of **Frame N+1**.
- Do not process a data-derived event twice or allow it to mutate state immediately upon generation outside the specified pipeline.

### AC-011 — Frame-local input
- OS input for Frame N is processed within Frame N. Do not silently enqueue it as a new input for Frame N+1.
- Continuing behavior in a later frame may use **persisted input/actor state** (e.g. held key/action in progress) or newly received OS input; this is not a fabricated replay of the previous input.
- Input and eligible data-derived impacts combine for the same target object through its impact-processing path.

### AC-012 — Object-centric impact aggregation
- Aggregate all applicable impacts **per target object**, e.g., player movement intent and collision/push effects.
- Resolve combined effects into the object's final proposed movement without independent systems separately moving the player.
- State changes are committed at the end-of-frame commit phase according to the existing implementation.
- The actual collision threshold and vector resolution formula must be read from source; do not infer a physical-force model from this illustration.

### AC-013 — Prototype movement model
- **Current phase:** final per-object movement output is a **displacement vector (ΔPosition)**.
- Position is updated in the end-of-frame commit stage using that final movement result.
- Velocity, acceleration and more realistic physics are **future work**, not authorized as part of this prototype's default tasks.
- Any later physics upgrade requires a separate owner-approved design; retaining the pipeline is preferable but not yet a decided implementation.

### AC-018 — Absolute ban on parallel state mutation
- **PROHIBITED:** new scripts/systems/components that update gameplay state through an independent loop bypassing the current aggregation and commit flow.
- **PROHIBITED:** additional Event Pipelines, State Managers or independent commits for the same object.
- **PROHIBITED:** direct position/state writes from new input, collision or feature code outside an authorized commit phase.
- This prohibition applies even to files **outside** the approved Core inventory. A new script is acceptable only as a compliant part of the established pipeline.

### AC-019 — Controlled new-event extension
- New event types/handlers may be added to existing extension points **without separate owner approval** if doing so does not modify protected core methods, change phase contracts, change platform behavior or establish a parallel system.
- If implementing an event requires a change to any core method or protected logic span, follow the pre-approval procedure below.

## 3. Protected code and approvals

### AC-008/014 — Core discovery and protection
- The Architecture Guardian performs a **read-only full source audit**, proposes an inventory of core systems with file/class/method evidence and dependency paths, and submits it to the owner.
- The Guardian **does not self-approve** Core classification. Until the owner approves an inventory, any plausible core change is treated as protected pending classification.
- Changes to core methods require owner approval even if they appear architecturally safe.

### AC-015/016 — Method-/logic-span-level authorization
For every proposed change to an approved (or provisionally protected) core method/logic span, provide **before making the edit**:
1. Exact file, class, method or code span and current behavior with source evidence.
2. Reason change is necessary.
3. Proposed behavior and how it preserves the phase/event/commit contract.
4. **Unapplied code diff** showing precise additions/deletions.
5. Dependency and risk analysis, including Editor impact.
6. Concrete test and regression plan.
7. A request for the owner's explicit approval of the named span.

- Approval applies **only** to the specific spans/diff scope authorized. New discovered edits require fresh approval.
- Guardian compares the final applied diff to the authorization; QA verifies behavior.
- Documentation-only reviews do not grant approval to edit source.

### AC-017 — Selective Guardian invocation
- Main Agent calls the Guardian for changes involving the core, uncertainty about core classification, or suspected violations of **core architecture invariants**.
- Ordinary non-core work may be handled by Main Agent + Game Engineer + QA without a mandatory Guardian invocation.
- Nevertheless **all agents must obey the Contract**. Guardian routing must not create a loophole for AC-018.

### AC-020/021 — Remediation scope
- The team may audit and repair **all confirmed architecture defects within the gameplay runtime flow**, not just defects directly related to the current feature.
- **Non-core** runtime violations may be fixed without owner signoff, while preserving the existing architecture and running regression tests.
- **Core** runtime violations still require approval down to the method/logic-span level.
- Do not broaden into arbitrary optimization or refactors unrelated to fixing a defect or completing supported gameplay behavior.

## 4. Editor isolation and runtime scene creation

### AC-021 — Protected level creation / edit tools
- Do not modify the **Level Creation**, **Level Editor**, or Editor import/export/UI/tooling features as part of runtime remediation.
- You may read their code to understand shared dependencies and test that they remain intact.
- When a file contains both runtime and Editor logic, follow method-level scope and preserve Editor methods and behavior.
- The protection includes existing Edit/Test Play entry and exit behavior; gameplay code shared by Play and Test Play may be fixed only without modifying Editor control flow.

### AC-022 — Script-driven hierarchy
- Game objects are instantiated/configured at runtime by scripts according to level data; manually configured pre-placed scene objects/prefabs are **not** the level's required source of truth.
- Do not require manually placing objects in the Unity Hierarchy for every JSON level.
- Reuse the present object-creation and lifecycle systems; do not install another object manager.
- Runtime-created Unity GameObjects may appear in the Hierarchy; this is expected and not contradictory.

## 5. Level load safety and QA

### AC-023/024 — Validate first, destroy old runtime objects, then create new ones
1. Read, parse and validate target level JSON (syntax, schema, supported types and data constraints) **before touching the currently active level**.
2. If invalid: report exact error and **retain the current playable level and gameplay state**.
3. If valid: stop/guard runtime use of old level, destroy level-owned runtime entities and clean associated temporary references/events.
4. Ensure no old level-owned entities/colliders remain active before creating the new level; honor Unity's actual destruction lifecycle rather than assuming `Destroy()` is immediately complete.
5. Use existing generic runtime construction to instantiate the new level. Only enable gameplay after successful initialization.
6. Never destroy unrelated persistent managers, the main event pipeline or Editor-owned objects as part of level cleanup.

### AC-025/026/027 — Initialization failure after valid JSON
- If object creation fails despite valid JSON, **stop level loading and gameplay safely**, clean partially created runtime level objects, and enter **Diagnostic Halt**.
- Display an error screen in place (do not automatically move to another scene), and preserve diagnostic logs with level ID, object/JSON path, phase, exception and stack trace when available.
- The prototype error screen has **no Retry and no Back to Main Menu**, and does not auto-reload. A developer diagnoses, repairs and restarts manually.
- A failure must not leave an incompletely initialized level playable.
- Implement the presentation within the existing code-driven UI architecture, without changing Level Editor functionality. Core modifications remain approval-gated.

### Object-level verification
- QA validates each JSON object description against actual runtime representation and behavior: supported type, position, dimensions, color, trajectory, collision behavior, object ownership/lifecycle and HTML reference as applicable.
- A JSON record is **not assumed** to map one-to-one to a Unity GameObject; determine the real representation from code.
- Test repeated level transitions (e.g. L01→L02→L03→L01), missing/invalid JSON, initialization errors, ghost/duplicate objects, stale colliders/events and Editor non-regression.
- Do not assert any test passed without runtime execution evidence. Static JSON validation and running gameplay verification are separate checks.

## 6. Roles and gates

**Main Agent / Project Orchestrator**: scopes tasks, identifies likely core impact, routes implementation and QA, enforces Contract on all tasks; may not override owner approvals.

**Systems Auditor**: reads project/HTML, maps real dependencies, reports observed behavior; does not invent new architecture.

**Gameplay Engineer**: implements approved gameplay changes through existing pipeline, edits non-core runtime when in scope, avoids Editor and unauthorized Core edits.

**QA & Release**: verifies logs, frame/event/commit ordering, JSON-to-runtime parity, level load failure safety, gameplay and non-regression; flags noncompliance as FAIL.

**Architecture Guardian**: read-only discovery/audit, method-level change proposals and unapplied diffs for Core, pre-implementation approval gate, post-implementation scope/diff check. It cannot issue its own owner approval.

### Decision routing
- **Platform behavior changes** → Owner approval required, regardless of core classification.
- **Core method/logic changes** → Guardian proposal + exact diff → Owner approval required.
- **New event at existing non-core extension point** → Implement if fully compliant, then QA.
- **Non-core runtime architecture defects** → May fix across gameplay runtime, then QA.
- **Editor creation/editing/tooling changes** → Out of scope; do not alter.
- **New independent update/state pipeline** → Absolutely prohibited, not a decision the agent may silently waive.

## 7. Required first read-only audit — not completed

The first Architecture Guardian run must generate **an evidence-backed report**, without modifying game code:

- Repository map covering Unity source, relevant HTML L01–L03 references, level JSON and Newtonsoft/Unity data conversion mechanisms when present.
- Proposed Core Systems table: path, class, method, owned state, readers/writers, phase, dependencies, risk, reason for classification.
- Event provenance map: OS input, persistent input state, data-derived events and next-frame queues.
- Per-frame execution map of actual aggregation and commit paths.
- Level loading lifecycle map: validation, cleanup, destroy, creation, failure handling.
- Inspector/Hierarchy and Editor dependency separation.
- Confirmed compliance, proven violations and **UNVERIFIED** items with line references.
- Explicit questions requiring the owner's classification/approval.

**Do not mark the audit as complete until evidence is available.** Do not infer method names or implementation from this document.

## 8. Governance

1. Only the project owner can approve changes to this Contract or to protected Core method/logic spans.
2. Preserve an audit trail: requirement → affected code → approved diff (if needed) → test evidence → final diff review.
3. In a conflict between an AI-generated suggestion and this Contract, follow the Contract and report the conflict.
4. If existing source contradicts a rule here, identify the discrepancy; follow the protection/approval rules instead of silently rewriting the architecture.
5. This Contract is the permanent project reference. `AGENTS.md` must direct Codex and subagents to consult it.

---

**Pending owner decisions:** Approval of the actual Core System inventory and resolution of any architecture-specific uncertainties uncovered by source audit. No agent configuration, Unity source modification or Editor change is implied by writing this document.
