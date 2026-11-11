# JSON parity report — resumed execution

Author/owner: json_engineer. Scope: canonical L01/L02/L03 JSON and this evidence directory. Classification: non-Core serialized data repair authorized by master task; no runtime methods, schema C#, Editor tooling, scene or .meta changes. Tests: actual Python source extraction, all-platform coordinate/material assertions, JSON parse/write/readback, and unchanged-section equality assertions.

## Executed repair

Canonical files: Assets/Resources/Levels/L01.json, L02.json, L03.json. Backups: matching *.before.json in this directory. SHA256 values and executed check output are retained in validation.json. Workspace write probe succeeded and was removed.

All seven collectibles previously had only x/y/w/h. LevelData.cs:69-76 declares CollectibleData.position and size instead. Added these consumed fields without removing authored raw fields. Conversion is center=((x+w/2)/64, -(y+h/2)/64), size=(w/64,h/64). HTML quangRebuild/quangRectsOverlap (2897-2903) confirms top-left rectangle coordinates; drawQuangItems uses q.x+q.w/2 and q.y+q.h/2 (2942). Example: L01 item_mvnivkti_1 is (1736,844,26,26) pixels, yielding center (27.328125,-13.390625), size (0.40625,0.40625).

Added newton.enabled=true and original vestibuleId to declared NewtonChamberData fields. L01 vestibuleId is item_mvhtg97m_h; L02/L03 are null. Existing Newton numeric values, rails, geometry, checkpoint positions, phase colors, and trajectories were preserved. Unsupported R/lambda were retained rather than silently dropped.

## Results

| Level | Platforms | Checkpoints excluding spawn | Collectibles | Actual static checks |
|---|---:|---:|---:|---|
| L01 | 16 | 5 | 2 | PASS |
| L02 | 15 | 4 | 2 | PASS |
| L03 | 27 | 8 | 3 | PASS |

PASS above means executed Python JSON parsing/readback, exact counts, all-platform geometry within 0.000001 Unity units of fixed HTML conversion and material mapping. It does not mean Unity JsonUtility or gameplay tests passed. Unity parsing and object construction are NOT_RUN by this agent; QA owns execution.

## Remaining limitations

- No data.collectibles consumer exists in LevelLoader or other runtime gameplay source found. Correct JSON alone does not instantiate collectible objects.
- LevelData has one checkpoint position for retry spawn/contact area. HTML has distinct zone center and respawn position; changing either to mimic the other falsifies data semantics. Requires protected schema/runtime proposal, not JSON guessing.
- L03 authored smooth motion is represented by existing linear PingPong trajectory data; easing behavior needs owner-approved platform change. Trajectories were not changed.
- Platform stable ids, material names, world mode/deathY, checkpoint id/zone/resetNewton, and precise phase are not represented by existing schema. Source HTML remains authoritative; these were not invented or discarded in a new conversion.
- Existing scratch/generate_level_json.py still generates the original mismatch. It was inspected, not run or modified, because delegated ownership is the canonical JSON only.
- JsonUtility ignores retained raw collectible x/y/w/h and unsupported Newton R/lambda. They are evidence, not claims of runtime consumption. Export through existing LevelData serialization can drop unsupported fields; Editor flow was not changed.
- L01/L02/L03 data parity is PARTIAL until these limitations and actual runtime loading are tested. Prior Reports/03_LEVEL_JSON_UPDATE.md claims of complete schema/Editor compatibility are not treated as runtime evidence.