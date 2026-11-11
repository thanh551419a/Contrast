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
