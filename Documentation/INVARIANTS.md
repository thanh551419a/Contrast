# System Invariants: Contrast

> **Target Audience:** All software engineers, gameplay programmers, and AI assistants. This document catalogs the **absolute, non-negotiable architectural rules and invariants** of the Contrast project. Violating any invariant listed here will destabilize the simulation, corrupt saved data, or introduce critical regressions.

---

## 1. Single Authoritative Gameplay Update Pipeline

```text
Invariant:
All gameplay physics, player input, moving platforms, and camera tracking MUST be pumped exclusively from GameManager.Update() during GameState.Playing.
```
- **Why it exists:** Eliminates non-deterministic race conditions and frame-to-frame execution order discrepancies between Unity's built-in `Update()` calls.
- **Where it is enforced:** [`GameManager.cs` lines 80–111](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Core/GameManager.cs#L80-L111).
- **What depends on it:** Platform AABB freshness, rider-carry calculation, predictive collision sliding, checkpoint activation.
- **What breaks if violated:** Moving platforms may pass through the player; rider displacement will lag by one frame; player could sink into platforms before collision resolution evaluates.

---

## 2. Axis-Aligned Platform Geometry (No GameObject Rotation)

```text
Invariant:
ColorPlatform GameObjects must NEVER be rotated in world space (transform.rotation == Quaternion.identity). Trajectory angles apply solely to mathematical evaluation equations.
```
- **Why it exists:** The custom physics engine is strictly axis-aligned (`Contrast.Player.Aabb`). It does not support oriented bounding boxes (OBB) or Gilbert-Johnson-Keerthi (GJK) collision.
- **Where it is enforced:** [`Trajectory.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs) rotates the evaluation vector mathematically (`localX * cos`, `localX * sin`); `ColorPlatform.cs` explicitly keeps `transform.rotation` unrotated.
- **What depends on it:** `ColorPlatform.GetAabb()`, `PlayerCollisionResolver`, `PlayerOverlapResolver`, resize handles in `LevelEditorController`.
- **What breaks if violated:** `ColorPlatform.GetAabb()` would return a degenerate or inaccurate bounding box; player would collide with invisible corners or fall through diagonal platforms.

---

## 3. Custom Mathematical Collision Over Physics2D

```text
Invariant:
Unity's Physics2D engine and 2D colliders must NEVER be used for authoritative gameplay collision. Rigidbody2D on the player must remain unsimulated (simulated = false).
```
- **Why it exists:** Contrast requires 100% deterministic, discrete pixel-precise collision and color permeability without Unity physics solver tunneling, bouncy impulses, or solver jitter.
- **Where it is enforced:** [`PlayerJumpMotion.Awake()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/Movement/PlayerJumpMotion.cs#L35) sets `rb.simulated = false;`.
- **What depends on it:** Custom sliding in `PlayerCollisionResolver`, overlap ejection in `PlayerOverlapResolver`, color transparency rules.
- **What breaks if violated:** Dual-engine conflict; player will experience erratic double-collisions, unwanted friction, or physics-induced rotation.

---

## 4. Centralized Transform Mutation Contract

```text
Invariant:
Normal gameplay movement must write to player transform.position ONLY through PlayerController.ApplyMove(). Movement resolvers must remain pure functional calculators.
```
- **Why it exists:** Ensures centralized logging, predictable state tracking, and eliminates contradictory transform writes from conflicting subsystems.
- **Where it is enforced:** [`PlayerController.ApplyMove()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs#L162). `PlayerMovementResolver`, `PlayerCollisionResolver`, and `PlayerOverlapResolver` return immutable structs.
- **What depends on it:** Position logging (`[POSITION]`), velocity capping, camera boundary clamping.
- **What breaks if violated:** Bypasses collision resolution; introduces sudden tunneling through walls or out-of-bounds clipping.

---

## 5. Authoritative Export Coordinates Contract

```text
Invariant:
When exporting a moving platform to JSON, the serialized position field MUST equal Trajectory.GetInitialPosition(trajData), NOT platform.Position or platform.AuthoredPosition.
```
- **Why it exists:** If a platform has `reverseDirection = true` or `SpatialBounds` clipping, its initial starting point ($t=0$ or $t=1$) differs from its origin anchor.
- **Where it is enforced:** [`LevelEditorController.BuildExportData()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L865).
- **What depends on it:** Round-trip level persistence, seamless level loading without frame-0 object teleportation.
- **What breaks if violated:** Platforms will spawn at an incorrect location upon level load and instantly teleport to the trajectory curve on frame 1.

---

## 6. Registry Synchronization & Lifecycle Integrity

```text
Invariant:
Every instantiated ColorPlatform and LevelMarker must register in ColorPlatform.All / LevelMarker.All, and MUST deregister immediately upon OnDestroy().
```
- **Why it exists:** High-frequency collision and interaction queries iterate over `ColorPlatform.All` every frame without performing expensive scene scans.
- **Where it is enforced:** `ColorPlatform.Initialize()`, `ColorPlatform.OnDestroy()` with `[ExecuteAlways]`, and `LevelMarker.Initialize()`, `LevelMarker.OnPlayerTouched()`.
- **What depends on it:** `PlayerCollisionResolver`, `PlayerOverlapResolver`, `TrajectorySystem.UpdateAll()`, `LevelEditorController.HitPlatform()`.
- **What breaks if violated:** A destroyed platform left in `All` triggers a fatal `NullReferenceException` inside the gameplay update loop.

---

## 7. Trajectory Phase Directionality & Reverse Integrity

```text
Invariant:
When resetting a trajectory with reverseDirection == true, phase MUST initialize to 1.0f and pingPongDir to -1.
```
- **Why it exists:** If `phase` is set to $0.0f$ when `reverseDirection == true`, the platform starts at the forward origin and instantly clamps or bounces on frame 1.
- **Where it is enforced:** [`Trajectory.ResetPhase()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/Trajectory.cs#L81-L82).
- **What depends on it:** Kinematic continuity, level timing synchronization, automated tests `Test_LineEvaluation_Reverse` and `Test_LineReverse`.
- **What breaks if violated:** Erroneous forward motion at level boot for reverse-configured platforms; failure of round-trip test assertions.

---

## 8. UI Interaction Guarding (No Viewport Drag Bleed)

```text
Invariant:
No editor viewport drag or camera pan operation may begin or continue if the mouse pointer is over an active UI element.
```
- **Why it exists:** Prevents accidental movement of platforms or camera while the user is clicking inspector buttons, typing in text boxes, or choosing dropdown items.
- **Where it is enforced:** [`LevelEditorController.Update()`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L370-L415) via `EventSystem.current.IsPointerOverGameObject()`.
- **What depends on it:** Editor usability, prevention of accidental level corruption.
- **What breaks if violated:** Clicking UI controls inadvertently selects and drags platforms situated underneath the panel.
