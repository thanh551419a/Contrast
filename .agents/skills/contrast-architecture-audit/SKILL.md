---
name: contrast-architecture-audit
description: Use when investigating Contrast Unity code ownership, state writers, runtime frame pipeline, Core candidates or Editor dependencies; evidence-only source audit.
---


# Architecture audit workflow
1. Read `AGENTS.md` and `docs/architecture/ARCHITECTURE_CONTRACT.md`.
2. Inventory authoritative source under `Assets/Script/Contrast`, data under `Assets/Resources/Levels`, HTML sources and relevant Unity `Assets/Editor` without changing them.
3. Trace actual input -> event -> impact -> commit with file:line proof, including Newton and trajectory paths; search for every state writer.
4. Mark each assertion VERIFIED / REQUIRED / DISCREPANCY / UNVERIFIED. A contractual phase is not proof of implementation.
5. Propose Core spans and Protected Editor spans separately; user approval is mandatory before Core classification is finalized.
6. Return concise dependency map and uncertainties. No changes.
