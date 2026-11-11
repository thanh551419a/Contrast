---
name: contrast-json-levels
description: Use for Contrast HTML-to-JSON level conversion and validation, including coordinates, schema, colors and generic level descriptions without Editor changes.
---


# JSON level contract
1. Read current C# LevelData and sanitizer, JSON Resources and HTML reference.
2. Verify exact JSON field names/types consumed by Unity, coordinate transforms and required/optional semantics.
3. Preserve original authored values and data-driven level representation; no per-level C# hardcoding.
4. Validate object-level position/size/color/trajectory/checkpoint/Newton and report unsupported mechanics.
5. Do not change Core schema/methods without Guardian-reviewed owner approval; never touch editor import/export.
6. Record before/after data checks and unrelated fields kept unchanged.
