---
name: contrast-unity-runtime
description: Use when implementing Unity gameplay changes in Contrast: LevelLoader, input, collision, movement, trajectories, object lifecycle and frame phases.
---


# Unity runtime implementation
1. Read contract and actual source; determine Core and touched methods before editing.
2. For Core edits, produce exact unapplied diff and stop for explicit per-method owner approval.
3. For platform behavior changes, owner approval regardless of Core.
4. Reuse current frame input/event aggregation; no parallel Update mutators, State Manager or independent commit.
5. Preserve prototype displacement-vector movement and the current 50% overlap semantics unless approved.
6. Leave Level Creation and Editor import/export/control flow untouched.
7. Run tests, inspect Console/Player logs; return exact source changes and evidence.
