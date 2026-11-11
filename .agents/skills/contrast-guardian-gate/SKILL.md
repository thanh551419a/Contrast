---
name: contrast-guardian-gate
description: Use when changes touch or may touch approved/provisional Core methods, platform behavior, frame/state invariants; prepare diff and require owner approval.
---


# Guardian method-level gate
1. Read architecture contract and actual source. Confirm proposed Core inventory is owner-approved before claiming final classification.
2. Detect whether any protected method, logic span, platform behavior or Core invariant would be affected directly or indirectly.
3. For every Core change, present existing method with evidence, intended behavior, exact **unapplied diff**, dependencies, Editor impact and tests.
4. STOP and request the owner's explicit approval naming each method/span. The Guardian cannot approve itself.
5. On approved implementation, compare final diff against approval; flag unauthorized spans; repeat approval for new scope.
6. Non-Core event extensions and runtime fixes can proceed if compliant; no independent state pipeline under any circumstances.
