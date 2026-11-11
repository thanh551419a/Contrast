---
name: contrast-newton
description: Use for Newton ring gameplay mechanics and UI in Contrast Unity: K/I controls, X marking, reveal progression and Newton camera/trajectory integration.
---


# Newton specialist workflow
1. Verify Newton behavior against HTML L01-L03, actual JSON and source; don't assume constants.
2. Trace ring coordinates/radii, mark placement, K/I semantics, capture/reveal and input frame timing.
3. Trace Newton Update and writes to Player/Trajectory/Camera; classify conflicts with existing phases.
4. Do not create an independent state-changing Update loop or modify shared Core methods without approved diffs.
5. Any change in platform trajectory/dock behavior requires explicit owner approval.
6. Verify visual behavior with runtime evidence and QA; source inspection alone is NOT_RUN.
