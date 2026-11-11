# SILHOUETTE MOTION — DEMO & PACKAGE ANALYSIS REPORT

## 1. Executive Summary

Before importing or modifying any files in the **CONTRAST** Unity project, a complete read-only inspection was performed on:
1. `C:\Users\thanh\Downloads\SilhouetteMotion-Unity\Demo` (inspected **first**, per mandatory requirement)
2. `C:\Users\thanh\Downloads\SilhouetteMotion-Unity\Assets\SilhouetteMotion`

The `SilhouetteMotion-Unity` package provides a 2D sprite-based minimalist silhouette character with three authored animation sheets (`Idle`, `Run`, `Jump`), JSON timing metadata, a `MotionLibrary` asset/sampler, `AnimationClip` generation (`Idle.anim`, `Run.anim`, `Jump.anim`), and a reference velocity-synchronized pose driver (`SilhouettePlayer.cs`).

---

## 2. Demo Folder Analysis (`SilhouetteMotion-Unity/Demo`)

### 2.1 Files Inspected
- `Demo/HUONG-DAN-UNITY.txt` (7,973 bytes) — Authoritative Vietnamese technical specification for the animation system, frame metrics, pivot alignment, and custom controller integration.
- `Demo/index.html` (13,210 bytes) — Self-contained HTML5 Canvas authoring and preview tool (`Silhouette / Motion lab`) that procedurally renders the silhouette (`Circular head, tapered torso, rounded solid limbs`, fill/stroke `#888987`) and exports deterministic `256×256` cell PNG sprite sheets and JSON timing descriptors.
- `Demo/preview.png` (51,424 bytes) & `Demo/reference.png` (8,108 bytes) — Visual reference renders of the character poses.

### 2.2 Character Visual Construction & Style
- **Visual Style**: Minimalist optical silhouette (`#888987` neutral gray RGB `(136, 137, 135)` with clean alpha transparency), allowing direct multiplicative/tint coloring (`SpriteRenderer.color`) for **White**, **Gray**, and **Black** phases in CONTRAST.
- **Cell Format**: Uncropped `256 × 256 px` RGBA32 cells arranged in 8-column sprite sheets (`columns = 8`, `cellSize = 256`).
- **Pixels Per Unit (PPU)**: `128 px/unit` (so each `256 × 256 px` cell is `2.0 × 2.0` raw Unity units at scale `1.0`).
- **Sprite Pivot**: Normalized `(0.5, 0.125)` from bottom-left, which corresponds to pixel `(128, 224)` from top-left (`y = 224` baseline in `256×256` cell). The character's planted feet rest directly at the pivot baseline `(0.5, 0.125)`.
- **Root Motion**: `rootMotion = false`. In `Demo/index.html` (`sheet()` function, line 68), all exported frames are rendered with `rootMotion = false`, meaning vertical jump translation (`q.y`) is **not** baked into the sprite frames. All character displacement is owned 100% by the gameplay controller.

### 2.3 Animation Clips & Timing Breakdown
| Clip / Mode | Frame Count | Duration (s) | Sample Rate (FPS) | Loop | Phase Breakdown |
| :--- | :---: | :---: | :---: | :---: | :--- |
| **`Idle`** | `120` | `2.40 s` | `50.0 fps` | `true` | Continuous sine breathing (`±1.8%` Y, `±0.81%` X counter-squash, `±1.5°` subtle arm sway), feet planted at baseline. |
| **`Run`** | `32` | `0.64 s` | `50.0 fps` | `true` | 2-step gait cycle (`3.125 steps/s`), `9°` forward torso lean, `3.5 px` hip bounce, `±34°` counter-phase arm swing, `±32°` leg swing, `±2.5%` body squash/stretch. |
| **`Jump`** | `61` | `1.22 s` | `50.0 fps` | `false` | Divided into 4 distinct sub-phases:<br>• **Anticipation (`0.00–0.06s`, 3 frames)**: Crouch squash (`-20%` Y, `+12%` X), arms swept back.<br>• **Airborne (`0.06–0.76s`, 35 frames)**: Launch stretch (`+13%` Y), rising pose (`u < 0.45`), apex pose (`0.45 <= u < 0.55`), and falling pose (`u >= 0.55`).<br>• **Landing (`0.76–0.94s`, 9 frames)**: Impact squash (`-20%` Y at 45ms, recovering over 135ms).<br>• **Rest (`0.94–1.22s`, 14 frames)**: Ready/neutral standing recovery. |

---

## 3. Assets Folder Analysis (`SilhouetteMotion-Unity/Assets/SilhouetteMotion`)

### 3.1 Scripts & Architecture in Package
1. **`MotionLibrary.cs` (`SilhouetteMotion.MotionLibrary`)**:
   - `ScriptableObject` storing `Sprite[] idle`, `Sprite[] run`, `Sprite[] jump`, and phase timing parameters (`idleDuration = 2.4f`, `runDuration = 0.64f`, `anticipation = 0.06f`, `air = 0.70f`, `landing = 0.18f`, `rest = 0.28f`).
   - Provides `Sprite Sample(string mode, float time)` which deterministically indexes into the corresponding frame array.
2. **`SilhouettePlayer.cs` (`SilhouetteMotion.SilhouettePlayer`)**:
   - Demo standalone controller (uses `Rigidbody2D` + `CapsuleCollider2D` for the demo scene only).
   - Demonstrates the recommended integration pattern documented in `HUONG-DAN-UNITY.txt` (lines 99–106):
     - **Facing Direction**: `visual.flipX = axis < 0` (flips only the child `SpriteRenderer`, never mutating the root collider or gameplay transform).
     - **Airborne Pose Synchronization**: Maps actual vertical velocity `v_y` into the airborne normalized parameter `u ∈ [0, 1]` (`u = v >= 0 ? 0.5f * (1 - Clamp01(v / jumpSpeed)) : 0.5f + 0.49f * Clamp01(-v / (jumpSpeed * 1.2f))`), sampling `Jump` at `anticipation + air * u`. This guarantees that rising, apex, and falling poses always match true physics regardless of jump height or platform drop.
     - **Landing Recovery**: Tracks `landTime` upon ground transition (`!wasGrounded && grounded`), sampling `Jump` at `anticipation + air + landTime` while `landTime < landing` (`0.18s`).
     - **Run vs Idle**: When grounded and post-landing, samples `"Run"` if moving horizontally (scaling `runClock` by horizontal speed ratio) or `"Idle"` when stationary.
3. **`Editor/MotionSetup.cs` (`SilhouetteMotion.Editor.MotionSetup`)**:
   - Slices `Sheets/{Idle,Run,Jump}.png` into individual frame textures under `Frames/{Idle,Run,Jump}/000.png ...`, configures `TextureImporter` (`PPU = 128`, `pivot = (0.5, 0.125)`, `FullRect`, `Bilinear`, `Uncompressed`), builds `Generated/MotionLibrary.asset`, and creates `Generated/Idle.anim`, `Generated/Run.anim`, and `Generated/Jump.anim`.

---

## 4. Integration Strategy for CONTRAST

Per `HUONG-DAN-UNITY.txt` (lines 99–106) and the Master Prompt requirements:
1. **Do NOT use `SilhouettePlayer.cs` or `MotionDemo.cs` in CONTRAST gameplay**: `PlayerController` in CONTRAST remains the single authoritative owner of Player movement, custom AABB collision, jump velocity, coyote time, jump buffer, moving platform rider displacement, and `deathY`.
2. **Import & Slice Sprite Sheets + Build `AnimationClip`s and `AnimatorController` / `MotionLibrary`**:
   - Import the `SilhouetteMotion` sprite sheets (`Idle.png`, `Run.png`, `Jump.png` and JSON metadata) and generate the sliced sprites, `AnimationClip`s (`Idle.anim`, `Run.anim`, `Jump.anim`), `AnimatorController` (`SilhouettePlayerController.controller`), and `MotionLibrary` asset.
   - Attach an `Animator` (`applyRootMotion = false`) and `SpriteRenderer` on the Player's child visual hierarchy (`PlayerVisualPresentation`), driving both the `Animator` state machine parameters (`Speed`, `IsGrounded`, `VerticalVelocity`, `IsLanding`, `IsDead`) and exact frame-accurate sprite synchronization (`MotionLibrary.Sample`) so that animations work seamlessly in both Editor/Standalone builds and runtime-instantiated levels without root motion interference.
   - Preserve `VisualPresentationRoot/OpticalBody` and `OpticalRim` compatibility for phase color tinting (`White`, `Gray` `#858585`, and `Black` `#050505` silhouette with `#74818b` rim outline) so the silhouette remains clearly legible against all backgrounds in `L01`, `L02`, and `L03`.

