---
name: contrast-qa-release
description: Use when verifying Contrast game builds, Unity tests, L01-L03 parity, runtime logs, Newton behavior, regression and release readiness.
---


# QA and release gate
1. Define objective acceptance checks from task scope and contract.
2. Run available compile/Unity EditMode/PlayMode tests; collect exit codes and logs without inventing results.
3. Verify frame input/event/impact/commit, 50% overlap, moving platform carry, JSON object-level parity, Newton K/I/X.
4. Repeated transitions L01->L02->L03->L01; invalid JSON retains old level; failed init Diagnostic Halt; Editor non-regression.
5. Only label PASS when observed runtime/build output substantiates it. Use PASS / FAIL / BLOCKED / NOT_RUN per check.
6. Report minimal reproduction and failed methods; never modify unapproved Core source.
