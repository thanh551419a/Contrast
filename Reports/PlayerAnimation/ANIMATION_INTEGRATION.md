# SILHOUETTE MOTION — PLAYER ANIMATION INTEGRATION REPORT

## 1. Imported Assets Inventory

Only the required character animation assets from `C:\Users\thanh\Downloads\SilhouetteMotion-Unity\Assets\SilhouetteMotion\Sheets` were imported into `Assets/Resources/SilhouetteMotion/`:

| Asset Path | Format | Details |
| :--- | :--- | :--- |
| `Assets/Resources/SilhouetteMotion/Sheets/Idle.png` | `2048 × 3840` RGBA32 (`128 PPU`) | `120` frames (`8 × 15` grid of `256×256` cells), `2.40s` loop |
| `Assets/Resources/SilhouetteMotion/Sheets/Idle.json` | JSON Metadata | `frameCount = 120`, `duration = 2.40s`, `fps = 50`, `pivot = (0.5, 0.125)`, `rootMotion = false` |
| `Assets/Resources/SilhouetteMotion/Sheets/Run.png` | `2048 × 1024` RGBA32 (`128 PPU`) | `32` frames (`8 × 4` grid of `256×256` cells), `0.64s` loop |
| `Assets/Resources/SilhouetteMotion/Sheets/Run.json` | JSON Metadata | `frameCount = 32`, `duration = 0.64s`, `fps = 50`, `pivot = (0.5, 0.125)`, `rootMotion = false` |
| `Assets/Resources/SilhouetteMotion/Sheets/Jump.png` | `2048 × 2048` RGBA32 (`128 PPU`) | `61` frames (`8 × 8` grid of `256×256` cells), `1.22s` non-loop |
| `Assets/Resources/SilhouetteMotion/Sheets/Jump.json` | JSON Metadata | `frameCount = 61`, `duration = 1.22s`, `fps = 50`, `pivot = (0.5, 0.125)`, `rootMotion = false` |
| `Assets/Resources/SilhouetteMotion/Idle.anim` | `AnimationClip` | `120` frames, `loopTime = true` |
| `Assets/Resources/SilhouetteMotion/Run.anim` | `AnimationClip` | `32` frames, `loopTime = true` |
| `Assets/Resources/SilhouetteMotion/Jump.anim` | `AnimationClip` | `61` frames, `loopTime = false` |
| `Assets/Resources/SilhouetteMotion/SilhouettePlayerAnimator.controller` | `AnimatorController` | States: `Idle`, `Run`, `Jump`, `Fall`, `Landing`, `Death` |

Unrelated demo gameplay scripts (`SilhouettePlayer.cs`, `MotionDemo.cs`, `MotionLab.unity`) were **not** imported into CONTRAST gameplay.

---

## 2. Runtime Hierarchy & Authoritative Separation

```text
Player (GameObject)
├── PlayerController (Authoritative movement, AABB collision, state, color phase, deathY)
├── PlayerInput / PlayerMovementResolver / PlayerCollisionResolver / PlayerJumpMotion
├── PlayerVisualPresentation (Presentation-only animation, scaling, & phase tinting)
└── VisualPresentationRoot (Child Transform anchored at Player collider feet)
    ├── Animator (applyRootMotion = false, SilhouettePlayerAnimator)
    ├── GlowHalo (SpriteRenderer, sortingOrder = 9, soft optical glow)
    ├── OpticalBody (SpriteRenderer, sortingOrder = 10, SilhouetteMotion character interior)
    ├── OpticalRim (SpriteRenderer, sortingOrder = 11, SilhouetteMotion 2px perimeter contour)
    └── CoreLight (SpriteRenderer, sortingOrder = 12, White-phase chest emitter)
```

- **Root Motion Disabled**: `characterAnimator.applyRootMotion = false` is enforced on initialization and every frame (`AdvanceAnimation` / `LateUpdate`).
- **Zero Physics Interference**: `PlayerVisualPresentation` never modifies `Player.transform.position`, `Player.transform.localScale`, `playerSize`, `playerOffset`, `moveSpeed`, jump impulse, gravity, or collision detection.

---

## 3. Animation State Mapping

| Gameplay Condition (`PlayerController`) | `PlayerAnimationState` | Package Clip | Frame / Time Selection |
| :--- | :--- | :--- | :--- |
| Grounded, `|selfVelX| <= 0.12`, post-landing | **`Idle`** | `Idle` (`120` frames) | Loops continuously (`idleClock % 2.40s`) |
| Grounded, `|selfVelX| > 0.12` (self-movement, excluding carrier delta) | **`Run`** | `Run` (`32` frames) | Loops with speed-scaled clock (`runClock % 0.64s`) |
| Airborne (`!IsGrounded`), `vy > 0.05` or `IsJumping` | **`Jump`** | `Jump` (`61` frames) | Velocity-mapped rising pose: `u = 0.5 * (1 - Clamp01(vy / 8.2))`, `t = 0.06 + 0.70 * u` |
| Airborne (`!IsGrounded`), `vy <= 0.05` | **`Fall`** | `Jump` (`61` frames) | Velocity-mapped apex/falling pose: `u = 0.5 + 0.49 * Clamp01(-vy / (8.2 * 1.2))`, `t = 0.06 + 0.70 * u` |
| Grounded transition (`!wasGrounded && isGrounded`), `landingTimer < 0.18s` | **`Landing`** | `Jump` (`61` frames) | Landing recovery frames (`t = 0.76s + landingTimer`) |
| `player.State == PlayerState.Dead` | **`Death`** | `Jump` (`61` frames) | Peak crouch compression pose (`t = 0.057s`) |
| `player.Respawn(...)` | **State-appropriate (`Idle`/`Fall`)** | `Idle` / `Jump` | Resets `landingTimer`, clocks, and restores immediate state |

---

## 4. Facing Direction & Phase Color Legibility

- **Facing Direction**:
  - Moving right (`selfVelX > 0.01f`) $\rightarrow$ `FacingDirection = +1` (`flipX = false`).
  - Moving left (`selfVelX < -0.01f`) $\rightarrow$ `FacingDirection = -1` (`flipX = true`).
  - Because each sprite frame is cropped symmetrically (`168 px` wide around `x = 128`, `VisualPivot = (0.5, 0.0)`), `flipX` mirrors the character in-place around the exact collider centerline without any horizontal shift.
- **Black / Gray / White Optical Phases**:
  - `OpticalBody` uses pure-white normalized silhouette frames so `SpriteRenderer.color` tints 1:1 to `White (#ffffff)`, `Gray (#858585)`, and `Black (#050505)`.
  - `OpticalRim` renders a 2-pixel contour stroke around the character's animated silhouette so that in `Black` phase (`#050505` body), the character has a crisp `#74818b` border against dark backgrounds.

