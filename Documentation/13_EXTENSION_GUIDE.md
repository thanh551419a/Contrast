# Extension Guide: Contrast

> **Target Audience:** Engineers and AI assistants planning to introduce new gameplay elements, platform properties, trajectory types, or editor tools without violating architectural invariants or breaking backward compatibility.

---

## 1. Extension Recipe: Adding a New Trajectory Curve Type

This recipe demonstrates adding a new parametric curve family (e.g., `Bézier` or `SineWave`).

```text
Files Likely Affected:
  1. Assets/Script/Contrast/Data/TrajectoryData.cs
  2. Assets/Script/Contrast/Level/Trajectory.cs
  3. Assets/Script/Contrast/Level/LevelEditorController.cs
  4. Assets/Script/Contrast/UI/LevelEditorUI.cs
  5. Tests/ContrastTests/Program.cs
  6. Assets/Editor/EditorLevelTests.cs
```

### Step 1: Update the Data Model (`TrajectoryData.cs`)
1. Extend `TrajectoryCurveType` enum:
   ```csharp
   public enum TrajectoryCurveType
   {
       Line = 0,
       Circle = 1,
       SineWave = 2 // New
   }
   ```
2. If new scalar parameters are required, add serialized fields with sensible defaults (e.g., `public float amplitude = 2f;`, `public float frequency = 1f;`).
3. Update `TrajectoryData.Clone()` to copy the new fields.

### Step 2: Implement the Evaluator (`Trajectory.cs`)
1. Add branch to `EvaluateWorldPoint()`:
   ```csharp
   case TrajectoryCurveType.SineWave:
       return EvaluateSineWave(cfg, t);
   ```
2. Implement static pure mathematical evaluation function:
   ```csharp
   private static Vector2 EvaluateSineWave(TrajectoryData cfg, float t)
   {
       Vector2 origin = new Vector2(cfg.originX, cfg.originY);
       float u = Mathf.Lerp(cfg.paramMin, cfg.paramMax, t);
       float localX = u * cfg.scaleX;
       float localY = Mathf.Sin(localX * cfg.frequency) * cfg.amplitude;
       // Apply rotation...
       return origin + rotated;
   }
   ```
3. Add validation branch in `Trajectory.Validate()` to reject invalid parameters (e.g., zero frequency).

### Step 3: Integrate with Level Editor Controller & UI
1. In `LevelEditorUI.cs`, update the `trajCurveType` dropdown options: `new string[] { "Line", "Circle", "SineWave" }`.
2. Update `LevelEditorUI.RepositionTrajectoryRows()` to display amplitude/frequency controls when `SineWave` is active.
3. Update `LevelEditorController.RefreshHandles()` to draw preview points (handled automatically by `GetPreviewPoints()`).

### Step 4: Verification & Automated Tests
1. Add automated test cases in `Tests/ContrastTests/Program.cs` and `Assets/Editor/EditorLevelTests.cs`.
2. Verify round-trip JSON serialization with new curve configuration.

---

## 2. Extension Recipe: Adding a New Platform Property

Suppose you want to add a `bounciness` or `friction` property to platforms:

```text
Files Likely Affected:
  1. Assets/Script/Contrast/Data/LevelData.cs (ColorPlatformData)
  2. Assets/Script/Contrast/Level/ColorPlatform.cs
  3. Assets/Script/Contrast/Level/LevelLoader.cs
  4. Assets/Script/Contrast/Player/Movement/PlayerCollisionResolver.cs
  5. Assets/Script/Contrast/UI/LevelEditorUI.cs
  6. Assets/Script/Contrast/Level/LevelEditorController.cs
```

### Step 1: Update DTO Schema (`LevelData.cs`)
```csharp
public class ColorPlatformData
{
    public Vector2Data position;
    public Vector2Data size;
    public float rawGrayscaleColor = 255f;
    public float bounciness = 0f; // New optional property (default 0)
    public TrajectoryData trajectory;
}
```
*Note: Because default is 0f, legacy JSON files will deserialize safely with bounciness = 0.*

### Step 2: Update Runtime Component (`ColorPlatform.cs`)
1. Add serialized field and public getter:
   ```csharp
   [SerializeField] private float bounciness = 0f;
   public float Bounciness => bounciness;
   ```
2. Update `Initialize()` and `SetEditorData()` signatures to accept `bounciness`.

### Step 3: Update Level Loader & Persistence
1. In `LevelLoader.BuildRuntimeLevel()`, pass `pData.bounciness` into `platform.Initialize()`.
2. In `LevelEditorController.BuildExportData()`, write `p.Bounciness` into `ColorPlatformData`.

### Step 4: Integrate with Collision Resolver
1. In `PlayerCollisionResolver.ResolveVertical()`, if platform contact occurs and `platform.Bounciness > 0`, reflect vertical velocity instead of clamping to zero.

### Step 5: Update Editor UI
1. Add an input field or slider in `LevelEditorUI.cs` under the `PLATFORM` inspector section.
2. Hook callback to `LevelEditorController.ApplySelectedPlatformFromUI()`.

---

## 3. Extension Recipe: Adding a New Level Marker Type

Suppose you want to add a `CollectableCoin` or `Hazard` marker:

```text
Files Likely Affected:
  1. Assets/Script/Contrast/Level/LevelMarker.cs (LevelMarkerType)
  2. Assets/Script/Contrast/Data/LevelData.cs
  3. Assets/Script/Contrast/Level/LevelLoader.cs
  4. Assets/Script/Contrast/Level/LevelEditorController.cs
  5. Assets/Script/Contrast/UI/LevelEditorUI.cs
```

### Implementation Checklist:
1. Add new enum value: `LevelMarkerType.Hazard`.
2. In `LevelData.cs`, add serializable collection: `public HazardData[] hazards;`.
3. In `LevelLoader.cs`:
   - Add spawning loop in `BuildRuntimeLevel()`.
   - Add contact detection branch in `ProcessPlayerInteractions()` (e.g., kill player upon touch).
4. In `LevelEditorUI.cs`:
   - Add a new palette button in `BuildPalette()`.
5. In `LevelEditorController.cs`:
   - Handle palette drag-and-drop instantiation.
   - Include new marker collection in `BuildExportData()`.

---

## 4. What Must NOT Be Done (Architectural Red Lines)

> [!CAUTION]
> The following actions violate core project invariants and will cause widespread regressions:

1. **DO NOT enable Unity Physics2D collision on platforms:**
   - Do NOT add active `BoxCollider2D` components expecting Unity physics to handle collisions. The entire gameplay model depends on `MovementGeometry.Aabb` and `PlayerCollisionResolver`.
2. **DO NOT rotate platform GameObjects:**
   - Never set `platform.transform.rotation = Quaternion.Euler(...)`. Trajectory orientation must be calculated in math; the platform AABB must remain strictly axis-aligned.
3. **DO NOT add independent `Update()` loops for gameplay entities:**
   - Never create autonomous `Update()` or `FixedUpdate()` scripts that move the player or platforms. All advancement must flow strictly through `GameManager.Update()`.
4. **DO NOT export raw `platform.Position` for moving platforms:**
   - Exporting `platform.Position` instead of `Trajectory.GetInitialPosition(cfg)` breaks reload alignment.
5. **DO NOT delete platforms by calling `Destroy(go)` directly:**
   - Always call `LevelEditorController.DeleteSelectedPlatform()` or ensure `ColorPlatform.All.Remove(this)` is called. Stale entries in `All` cause fatal `NullReferenceException` crashes in collision routines.
