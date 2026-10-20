# Known Issues & Technical Debt: Contrast

> **Target Audience:** Architects, engineers, and AI maintenance agents reviewing identified technical debt, limitations, known workarounds, and resolved defects across the codebase.

---

## 1. Active Issues & Technical Debt

### ISSUE-ACT-01: Dangling Component Reference in Scene File
- **Category:** Technical Debt
- **Description:** `Assets/Scenes/SampleScene.unity` contains a missing component reference on the `GameSystems` GameObject pointing to a deleted script with GUID `797f4f9bbeb9abe48868f6ec62002cb9` (`Contrast.UI.UIManager`).
- **Evidence:** `SampleScene.unity` line 627–629:
  ```yaml
  m_Script: {fileID: 11500000, guid: 797f4f9bbeb9abe48868f6ec62002cb9, type: 3}
  m_EditorClassIdentifier: Assembly-CSharp::Contrast.UI.UIManager
  ```
  File search confirms `UIManager.cs` does not exist in the project repository.
- **Related File:** [`Assets/Scenes/SampleScene.unity`](file:///y:/Contrast/My%20project/Assets/Scenes/SampleScene.unity)
- **Related Class:** `Contrast.UI.UIManager` (Missing)
- **Related Module:** [Core Module](MODULES/Core.md)
- **Impact:** Low. Produces a benign Unity Editor warning ("The referenced script on this Behaviour is missing!"). `GameManager.Awake()` handles all UI creation dynamically, so runtime execution is unimpeded.
- **Notes:** Can be safely cleaned up in Unity Inspector by removing the missing component from `GameSystems`.

---

### ISSUE-ACT-02: CheckPoint Schema Format Divergence
- **Category:** Known Limitation / Backward Compatibility Debt
- **Description:** Older level files (`SampleLevel.json`, `ContrastLevel.json`) store `"checkPoint"` as a single JSON object (`"checkPoint": { "position": ... }`), whereas modern levels (`SampleLevel.CheckpointEnd.Test.json`, `TrajectoryShowcaseLevel.json`) store it as an array (`"checkPoint": [ { ... } ]`).
- **Evidence:** [`LevelData.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/LevelData.cs#L13) declares `public CheckPointData[] checkPoint;`. Unity's `JsonUtility` fails to parse a JSON object into an array field, leaving `checkPoint = null`.
- **Related File:** [`Assets/Script/Contrast/Data/LevelData.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Data/LevelData.cs), [`Assets/Script/Contrast/Level/LevelLoader.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs)
- **Related Class:** `LevelData`, `LevelLoader`
- **Related Module:** [LevelData Module](MODULES/LevelData.md)
- **Impact:** Medium. In older levels formatted with a single checkpoint object, checkpoints are not spawned at runtime. The level still runs, but retry respawn falls back to `startPos`.
- **Notes:** Resolved in newly authored levels by wrapping checkpoint entries in square brackets `[ ... ]`.

---

### ISSUE-ACT-03: Lack of Multi-Level Undo/Redo in Level Editor
- **Category:** Known Limitation / Incomplete Feature
- **Description:** The Level Editor does not maintain an action history stack (`CommandManager`). Accidental platform body translation, resize handle adjustments, or property changes cannot be undone via `Ctrl+Z`.
- **Evidence:** Confirmed in `LevelEditorController.cs`. There is no `ICommand` or history list implementation. Noted as Rank 1 priority in `EDIT_LEVEL_REVIEW_COUNCIL.md`.
- **Related File:** [`Assets/Script/Contrast/Level/LevelEditorController.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs)
- **Related Class:** `LevelEditorController`
- **Related Module:** [LevelEditor Module](MODULES/LevelEditor.md)
- **Impact:** Usability. Designers must manually reposition or retype properties if an accidental adjustment is made. Mitigated for deletions via the two-step confirmation system.

---

### ISSUE-ACT-04: Viewport Lacks Modifier Grid Snapping
- **Category:** Known Limitation
- **Description:** Dragging platform bodies or resize handles in the editor viewport moves them with continuous floating-point coordinates without optional grid snapping (e.g., holding `Ctrl` or `Shift` to snap to $0.5$ or $1.0$ unit increments).
- **Evidence:** `LevelEditorController.ContinueMapPointer()` applies continuous cursor delta directly without a `Mathf.Round` snapping stage.
- **Related File:** [`Assets/Script/Contrast/Level/LevelEditorController.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L520-L687)
- **Related Class:** `LevelEditorController`
- **Related Module:** [LevelEditor Module](MODULES/LevelEditor.md)
- **Impact:** Minor. Designers desiring integer coordinates must manually type them into the Inspector input fields.

---

### ISSUE-ACT-05: Instant Velocity Discontinuity at PingPong Turnaround
- **Category:** Known Limitation / Kinematic Design Characteristic
- **Description:** In `PingPong` trajectory mode, platform velocity reverses instantaneously ($\Delta v = 2v$) with zero deceleration or easing curve.
- **Evidence:** Evaluated and approved by Physics and Mathematics Review Council (`EDIT_LEVEL_REVIEW_COUNCIL.md` §3, Reviewer 2).
- **Related File:** [`Assets/Script/Contrast/Level/Trajectory.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs#L149-L185)
- **Related Class:** `Trajectory`
- **Related Module:** [Trajectory Module](MODULES/Trajectory.md)
- **Impact:** Intentional retro design choice for precision puzzle platformers (such as *Super Meat Boy* / *Celeste*), allowing players to calculate cycle timings deterministically. Higher-order easing (Sine, Quad) is scheduled as a future enhancement.

---

## 2. History of Resolved Defects (Audited & Patched)

For reference, the following critical bugs were previously diagnosed, patched, and verified in October 2026:

| Defect ID | Summary | Root Cause | Fix Applied | Verification |
|---|---|---|---|---|
| **ISSUE-FIX-01** | Trajectory Reverse Phase Glitch | `phase` was unconditionally set to `0f` in `ResetPhase()`, causing platforms with `reverseDirection = true` to bounce on frame 1. | Initialized `phase = config.reverseDirection ? 1f : 0f` and `pingPongDir = config.reverseDirection ? -1 : 1`. | **PASS** in test `LineReverse` |
| **ISSUE-FIX-02** | Export Coordinates Teleportation | `BuildExportData()` exported `p.AuthoredPosition` instead of initial trajectory point, causing teleportation on load. | Export now defaults to `Trajectory.GetInitialPosition(trajData)` when trajectory is active. | **PASS** in test `RoundTrip` |
| **ISSUE-FIX-03** | Lack of In-Editor Test Play | Level editor could not test levels without exporting to disk. | Added `GameManager.StartGameWithLevelData()` and **TEST PLAY** button. | **PASS** in `LevelEditorUI` |
| **ISSUE-FIX-04** | Camera Pan Jitter | Incremental `ScreenToWorldPoint` delta accumulated floating-point drift. | Cached `panStartScreen` and initial camera position at mouse-down. | **PASS** in viewport tests |
| **ISSUE-FIX-05** | Edit Mode Registry Leak | `ColorPlatform` lacked `[ExecuteAlways]`, leaking destroyed instances in `All`. | Added `[ExecuteAlways]` to `ColorPlatform.cs`. | **PASS** in batchmode tests |
