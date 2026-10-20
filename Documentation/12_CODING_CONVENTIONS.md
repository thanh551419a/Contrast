# Coding Conventions: Contrast

> **Target Audience:** Engineers and AI agents contributing code to the Contrast repository. This guide documents the coding styles, patterns, conventions, and architectural idioms **actually practiced** across the codebase.

---

## 1. File & Folder Organization

- **Root Namespace Folder:** All gameplay source code resides under `Assets/Script/Contrast/`.
- **Sub-Namespace Structure:** Every folder under `Contrast/` corresponds 1:1 with a C# namespace:
  - `Contrast.Camera` $\to$ `Assets/Script/Contrast/Camera/`
  - `Contrast.Color` $\to$ `Assets/Script/Contrast/Color/`
  - `Contrast.Core` $\to$ `Assets/Script/Contrast/Core/`
  - `Contrast.Data` $\to$ `Assets/Script/Contrast/Data/`
  - `Contrast.Level` $\to$ `Assets/Script/Contrast/Level/`
  - `Contrast.Player` $\to$ `Assets/Script/Contrast/Player/`
  - `Contrast.UI` $\to$ `Assets/Script/Contrast/UI/`
  - `Contrast.Tests` $\to$ `Assets/Editor/EditorLevelTests.cs`
- **Editor Scripts:** Placed strictly in `Assets/Editor/` or guarded by `#if UNITY_EDITOR`.
- **One Primary Class Per File:** File name matches class name with one legacy exception: `GameOverui.cs` contains `public class GameOverUI`.

---

## 2. Naming Conventions

### 2.1 Symbols & Types
- **Classes & Structs:** `PascalCase` (e.g., `PlayerController`, `ColorPlatform`, `Aabb`, `MovementResult`).
- **Interfaces:** `PascalCase` with prefix `I` (e.g., `IPointerDownHandler`).
- **Enums:** `PascalCase` for enum type and its members (e.g., `GameState.Playing`, `LogicalColor.Universal`).
- **Methods:** `PascalCase` (e.g., `ProcessUpdate`, `BuildRuntimeLevel`, `EvaluateWorldPoint`).
- **Public Properties:** `PascalCase` (e.g., `Position`, `Size`, `CurrentColor`).
- **Private / Protected Fields:** `camelCase` (e.g., `playerSize`, `moveSpeed`, `statusText`).
- **Serialized Private Fields:** `[SerializeField] private float jumpHeight = 3f;` (camelCase, no `m_` prefix).
- **Constants:** `PascalCase` (e.g., `WhiteMin = 0f`, `UniversalValue = 255f`, `VisualOverlap = 0.02f`).

---

## 3. Class & Component Architecture

### 3.1 Component Attributes
- **Class Sealing:** Sealed classes are preferred when inheritance is not intended:
  ```csharp
  public sealed class PlayerController : MonoBehaviour
  public sealed class GameManager : MonoBehaviour
  public sealed class LevelEditorController : MonoBehaviour
  ```
- **Component Requirements:** Dependencies are explicitly declared on MonoBehaviours:
  ```csharp
  [RequireComponent(typeof(PlayerInput))]
  [RequireComponent(typeof(PlayerMovementResolver))]
  [RequireComponent(typeof(PlayerJumpMotion))]
  [RequireComponent(typeof(Rigidbody2D))]
  public sealed class PlayerController : MonoBehaviour
  ```
- **Execution Attributes:**
  - `[ExecuteAlways]` on `ColorPlatform.cs` to ensure `OnDestroy()` executes in Edit Mode.
  - `[DisallowMultipleComponent]` on unique controllers (`Trajectory`, `LevelMarker`, `PlayerCollisionResolver`).

### 3.2 Value Types for Intermediate Mathematics
- Pure calculation results use immutable structs rather than heap allocations:
  ```csharp
  public readonly struct MovementResult
  public readonly struct CollisionResult
  public readonly struct OverlapResult
  public readonly struct PlayerInputIntent
  internal readonly struct Aabb
  ```

---

## 4. Input Handling Idioms

The codebase employs dual-mode compilation to support modern Unity Input System while gracefully falling back to legacy Input:

```csharp
#if ENABLE_INPUT_SYSTEM
    Keyboard kb = Keyboard.current;
    if (kb != null)
    {
        float inputX = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        if (kb.spaceKey.wasPressedThisFrame && isGrounded)
            jumpRequested = true;
    }
#else
    float inputX = 0f;
    if (Input.GetKey(KeyCode.D)) inputX += 1f;
    if (Input.GetKey(KeyCode.A)) inputX -= 1f;
    if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        jumpRequested = true;
#endif
```

---

## 5. UI Guarding & Event Prevention

To prevent viewport drags when interacting with UI panels, all editor controllers must query:

```csharp
private static bool IsPointerOverUi()
{
    return EventSystem.current != null &&
           EventSystem.current.IsPointerOverGameObject();
}
```

If the cursor enters UI space during an active canvas drag, the drag is cancelled immediately:
```csharp
if (pointerOverUi)
    CancelMapPointerDrag("Pointer entered UI while dragging");
```

---

## 6. Logging & Diagnostics Conventions

All console logging uses standardized prefix tags for filtering in Unity Console or terminal output:

| Log Tag Prefix | Meaning / Subsystem | Example |
|---|---|---|
| `[FLOW]` | High-level state transition or major lifecycle event | `[FLOW][START_GAME] entering gameplay flow` |
| `[INPUT]` | Player or editor raw input reception | `[INPUT] inputX=1.00 moveIntent=(0.10, 0.00)` |
| `[OVERLAP]` | Anti-stuck and escape vector calculations | `[OVERLAP][DECISION] platform=ColorPlatform_1 12.50% => CANDIDATE_PUSH` |
| `[COLLISION]` | AABB slide clamping steps | `[COLLISION][X] BLOCK platform=ColorPlatform_0 correctedX=4.200` |
| `[POSITION]` | Centralized transform write | `[POSITION] (0.0, 1.0, 0.0) + (0.1, 0.0) = (0.1, 1.0, 0.0)` |
| `[CHECKPOINT]` | Checkpoint activation | `[CHECKPOINT] reached position=(10.0, 1.0) color=White` |
| `[EDITOR]` | Editor actions (drag, pan, selection, delete) | `[EDITOR][DELETE] platform=ColorPlatform_3` |
| `[TEST]` | Automated assertion report | `[TEST][PASS] LineForward_Start - Origin at phase 0` |

---

## 7. Serialization Guidelines

- All DTO classes use `[System.Serializable]`.
- Field names in serialized DTOs are `camelCase` to match existing JSON schemas (`startPos`, `checkPoint`, `rawGrayscaleColor`, `reverseDirection`).
- Unity's `JsonUtility` serializes C# enums as their underlying `int` values. When designing new enum settings, ensure index 0 represents the sensible default.
- Never use polymorphic class hierarchies or C# `Dictionary` inside objects serialized via `JsonUtility`.
