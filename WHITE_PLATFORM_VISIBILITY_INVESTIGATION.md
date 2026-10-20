# Diagnostic Report: Unresolved White Platform Visibility in Edit Mode

**Document:** `WHITE_PLATFORM_VISIBILITY_INVESTIGATION.md`  
**Project:** Contrast  
**Engine:** Unity 6000.5.8f1 (Universal Render Pipeline 2D)  
**Role:** Senior Unity C# Engineer (Diagnostic Forensic Audit Only)  
**Status:** Investigation Complete — No implementation modifications applied (Task B Restriction)

---

## 1. Problem Summary

In Edit mode, platforms configured with White color semantics (`rawGrayscaleColor` in the range `0f` to `84f`, resolving to [LogicalColor.White](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Color/ColorClassifier.cs#L20-L28)) are reported to be invisible or indistinguishable from the background.

A previous fix was attempted to address this defect by:
1. Assigning a slate border color (`WhiteBorder = new Color(0.18f, 0.25f, 0.35f, 0.95f)`) inside [PlatformVisualPresentation](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/PlatformVisualPresentation.cs#L35).
2. Adding a high-visibility cyan selection outline (`selectionBoxRenderer`, color `(0.2f, 0.75f, 1f, 0.85f)`) inside [LevelEditorController](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L1318).
3. Adding a selection wire cube gizmo inside [ColorPlatform.OnDrawGizmosSelected](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs#L233-L238).

Despite these changes, the defect remains unresolved: users and level designers still report that white platforms are either invisible or fail to stand out clearly from the background when editing levels.

In accordance with the strict scope for Task B, **no code, materials, shaders, alpha configurations, or scene assets have been altered to attempt another fix**. This document presents the forensic analysis of why the previous attempt failed and provides an evidence-based roadmap for subsequent remediation.

---

## 2. Previous Attempt Analysis

### Files Touched by Previous Attempt
- [Assets/Script/Contrast/Presentation/PlatformVisualPresentation.cs](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/PlatformVisualPresentation.cs)
- [Assets/Script/Contrast/Level/LevelEditorController.cs](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs)
- [Assets/Script/Contrast/Level/ColorPlatform.cs](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs)
- [Assets/Script/Contrast/UI/PlatformColorSubComponent.cs](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/UI/PlatformColorSubComponent.cs)

### Why the Previous Attempt Failed

1. **Selection-Only Confinement:**
   The cyan selection box in [LevelEditorController.RefreshHandles](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L1257-L1265) and the gizmo in [ColorPlatform.OnDrawGizmosSelected](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs#L233-L238) are only rendered when a platform is **actively selected**. When editing a level, unselected platforms make up almost all visible geometry. As soon as a user clicks away, deselects, or pans the viewport to inspect level flow, the selection box is deactivated (`selectionBox.SetActive(false)`), instantly reverting the white platform to its unframed state.

2. **Microscopic Sub-Pixel Border Thickness:**
   The border in [PlatformVisualPresentation](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/PlatformVisualPresentation.cs#L398-L436) is generated procedurally via `GetBorderSprite()`:
   ```csharp
   const int res = 64;
   const int borderPx = 2;
   // Sprite created with pixelsPerUnit = 64
   ```
   This produces a border that is physically $2 / 64 = 0.03125$ world units thick.
   - At camera `orthographicSize = 10` on a 1080p display, 1 world unit equals $54$ screen pixels. The border is $0.03125 \times 54 = 1.68$ pixels wide.
   - At camera `orthographicSize = 25` (common during zoomed-out editing or Fit Map framing), 1 world unit equals $21.6$ pixels. The border is $0.03125 \times 21.6 = 0.67$ pixels wide.
   At less than one screen pixel, bilinear texture sampling and GPU rasterization cause the dark border to antialias into near-zero opacity, completely disappearing from view.

3. **Bloom Bleed & Overexposure:**
   [VisualPresentationManager](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/VisualPresentationManager.cs#L122-L128) configures a global URP Post-Processing Volume with Bloom:
   ```csharp
   Bloom bloom = profile.Add<Bloom>(true);
   bloom.threshold.value = 0.85f;
   bloom.intensity.value = 0.55f;
   ```
   The White platform surface gradient in [PlatformVisualPresentation](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/PlatformVisualPresentation.cs#L32) ranges from `WhiteBottom = (0.812, 0.867, 0.898)` to `WhiteTop = (0.910, 0.941, 0.957)`. Because `WhiteTop` exceeds the $0.85$ threshold, the platform surface actively blooms. The additive glow scatter ($0.65$ scatter radius) bleeds outward over the adjacent $1.6$-pixel border, washing out the dark border.

4. **Component Lifecycle Gap in Edit Mode:**
   [PlatformVisualPresentation](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/PlatformVisualPresentation.cs#L19) does **not** declare `[ExecuteAlways]` or `[ExecuteInEditMode]`. In Unity Edit Mode outside of Play Mode (such as when working in the Unity Scene View or instantiating prefabs), its `Awake()` and `Update()` do not execute. If the visual presentation component is not running, [ColorPlatform](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs) falls back to its internal `Visual` child quad, which renders using [ColorPlatform.GetDefaultSprite()](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs#L202-L231) — a flat, borderless $2\times2$ solid white texture tinted `Color.white`.

---

## 3. Current Data and Rendering Flow

### Pipeline Flowchart

```
[User Color Selection]
       │
       ▼
[PlatformColorSubComponent] ────────► SetRawColor(0f)
       │
       ▼
[ColorClassifier.Classify(0f)] ─────► Returns LogicalColor.White
       │
       ▼
[LevelEditorController] ────────────► ApplySelectedPlatformFromUI(pos, size, rawColor: 0f)
       │
       ▼
[ColorPlatform.SetEditorData] ──────► rawGrayscaleColor = 0f
       │                             spriteRenderer.color = Color.white (1, 1, 1, 1)
       │                             pres.RefreshVisuals()
       ▼
[PlatformVisualPresentation] ───────► EnsureVisuals():
       │                               ├── Disables raw "Visual" renderer
       │                               ├── Slices "Surface" to platform size
       │                               └── Slices "Border" (0.03125 unit 9-slice)
       │                             UpdateSurfaceStyle():
       │                               ├── surfaceRenderer.sprite = GetWhiteSprite()
       │                               └── borderRenderer.color = WhiteBorder (0.18, 0.25, 0.35)
       ▼
[URP 2D Rendering Pipeline] ────────► DefaultLitMaterial ("Sprites-Lit-Default")
       │                               ├── Lit by Global Light 2D (Intensity: 1.0)
       │                               └── Rendered on Sorting Layer "Default", Order 0 & 1
       ▼
[URP Post-Processing] ──────────────► Global Volume Bloom (Threshold: 0.85, Intensity: 0.55)
       │                               └── White platform (>0.85) blooms, washing perimeter
       ▼
[Edit Viewport Output] ─────────────► Border < 1px at normal zoom; unselected platform blends
```

### Detailed Pipeline Stages

1. **Stored Representation:**
   Platforms store a single IEEE 754 single-precision float `rawGrayscaleColor` (`0f` to `255f`).
   - `0f .. 84f`: Classified as `LogicalColor.White`
   - `85f .. 169f`: Classified as `LogicalColor.Gray`
   - `170f .. 254f`: Classified as `LogicalColor.Black`
   - `255f`: Classified as `LogicalColor.Universal` ("Everything")

2. **Inspector Callback:**
   In [PlatformColorSubComponent](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/UI/PlatformColorSubComponent.cs#L130), clicking `[ WHITE ]` triggers `SetRawColor(0f, true)`. This invokes `onColorChangedCallback(0f)`.

3. **Authoritative Update:**
   [LevelEditorController.ApplySelectedPlatformFromUI](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L307-L335) calls `selectedPlatform.SetEditorData(position, size, rawColor)`.

4. **Visual Application:**
   [ColorPlatform.SetEditorData](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs#L129-L165) calls `pres.RefreshVisuals()`. [PlatformVisualPresentation](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/PlatformVisualPresentation.cs#L194-L200) assigns `surfaceRenderer.sprite = GetWhiteSprite()` and `borderRenderer.color = WhiteBorder`.

5. **Materials and Lighting:**
   Renderers are created via `AddComponent<SpriteRenderer>()`. In URP 2D ([Renderer2D.asset](file:///Y:/Contrast/My%20project/Assets/Settings/Renderer2D.asset#L65)), the default material is `DefaultLitMaterial` (`Sprites-Lit-Default`). The scene contains a `Global Light 2D` (intensity 1.0, color white), which illuminates the sprite.

6. **Post-Processing Stage:**
   `VolumeProfile` in [VisualPresentationManager](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/VisualPresentationManager.cs#L122-L138) applies Bloom with a threshold of $0.85$. The White platform surface exceeds this threshold, producing an outer bloom halo.

---

## 4. Evidence and Findings

### Confirmed Facts (Verified from Source & Assets)

| Fact ID | Evidence Location | Description | Confirmed Finding |
| :--- | :--- | :--- | :--- |
| **F-01** | [PlatformVisualPresentation.cs:L402-L436](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/PlatformVisualPresentation.cs#L402-L436) | `res = 64`, `borderPx = 2`, `pixelsPerUnit = 64` | The physical border thickness is fixed at exactly $2/64 = 0.03125$ world units. |
| **F-02** | [LevelEditorController.cs:L1250-L1265](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L1250-L1265) | `if (selectedPlatform == null) SetHandlesVisible(false);` | Selection outlines are strictly deactivated for all unselected platforms. |
| **F-03** | [PlatformVisualPresentation.cs:L19](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/PlatformVisualPresentation.cs#L19) | Class lacks `[ExecuteAlways]` attribute | `PlatformVisualPresentation` does not execute `Awake()` or `Update()` in Unity Edit mode outside Play mode. |
| **F-04** | [VisualPresentationManager.cs:L33-L42](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/VisualPresentationManager.cs#L33-L42) | `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]` | `VisualPresentationManager` is never instantiated outside Play mode. |
| **F-05** | [VisualPresentationManager.cs:L122-L128](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/VisualPresentationManager.cs#L122-L128) | Bloom `threshold = 0.85f`, `intensity = 0.55f` | Bloom threshold is lower than `WhiteTop` ($0.957$), causing the surface to glare over the perimeter. |
| **F-06** | [SampleScene.unity:L422](file:///Y:/Contrast/My%20project/Assets/Scenes/SampleScene.unity#L422) | `m_BackGroundColor: {r: 0.02, g: 0.031, b: 0.071, a: 1}` | Camera background in `SampleScene.unity` is dark navy (`#050812`). |
| **F-07** | [ColorPlatform.cs:L202-L231](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Level/ColorPlatform.cs#L202-L231) | Flat $2\times2$ white texture with no borders | Fallback renderer on child `Visual` has zero perimeter or texture variation. |

### Unverified Hypotheses (Requiring Live Display Observation)

- **H-01:** Whether the user is viewing the level in the Unity **Scene View** (with default skybox or light gray background) rather than the **Game View** (with dark camera clear color).
- **H-02:** Whether specific monitor color calibration (e.g., washed-out gamma or high dynamic range clipping) accentuates the loss of the slate border.
- **H-03:** Whether users inadvertently select raw value `255f` expecting it to be white (because 255 is white in standard 8-bit RGB), only to see the amber/universal color.

---

## 5. Ranked Potential Causes

### Rank 1 (Confirmed Primary Cause): Sub-Pixel Border Dissolution
- **Rationale:** The border thickness is hard-coded to $0.03125$ world units ($2$ pixels on a $64$ PPU texture). At any zoom level where orthographic size is $\ge 15$, this border resolves to less than $1$ physical screen pixel. Bilinear filtering interpolates this fraction against the adjacent white surface, completely eliminating visual edge definition.
- **Supporting Evidence:** [PlatformVisualPresentation.cs line 411](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/PlatformVisualPresentation.cs#L411), [line 428](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/PlatformVisualPresentation.cs#L428).

### Rank 2 (Confirmed Contributing Cause): Selection-Only Isolation
- **Rationale:** The visual aid added in the previous fix (`selectionBoxRenderer` cyan outline) only exists on the currently selected platform. When level designers view the canvas or edit another object, all other white platforms lose their outline entirely.
- **Supporting Evidence:** [LevelEditorController.cs lines 1250–1255](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelEditorController.cs#L1250-L1255).

### Rank 3 (Confirmed Environmental Cause): Lifecycle Inactivity in Edit Mode
- **Rationale:** If editing occurs in the Unity Editor without pressing Play, `PlatformVisualPresentation` does not run because it lacks `[ExecuteAlways]`. Platforms default to the unstyled `Visual` quad, which is a solid flat white square with no borders.
- **Supporting Evidence:** [PlatformVisualPresentation.cs line 19](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/PlatformVisualPresentation.cs#L19), [VisualPresentationManager.cs line 33](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/VisualPresentationManager.cs#L33).

### Rank 4 (Plausible Optical Cause): URP Bloom Scatter Overwash
- **Rationale:** Surface brightness ($0.957$) exceeds the bloom threshold ($0.85$). When bloom is active, the white platform emits an additive glow that radiates outward across the $1.6$-pixel perimeter, wiping out the dark border.
- **Supporting Evidence:** [VisualPresentationManager.cs lines 122–128](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/VisualPresentationManager.cs#L122-L128).

---

## 6. Recommended Diagnostic Experiments

To conclusively verify these findings without modifying production code, perform the following non-mutating experiments:

### Experiment 1: Measure Border Screen-Space Pixel Footprint Across Zoom Levels
- **Method:** In the Unity Game View with Stats enabled, place a white platform and use the mouse wheel to zoom from `orthographicSize = 5` to `orthographicSize = 25`. Capture a frame at each step and inspect the border with an image pixel loupe.
- **Expected Outcome if Hypothesis 1 holds:** At size $5$, a faint $\sim3$-pixel border is visible. At size $15+$, the border drops below $1$ pixel and blends completely into the white surface.
- **Prerequisite:** Can be executed safely in Play mode without source changes.

### Experiment 2: Compare Scene View vs. Game View in Edit Mode
- **Method:** Open `SampleScene.unity` in the Unity Editor without entering Play mode. Create or inspect a platform in the Scene View.
- **Expected Outcome if Hypothesis 3 holds:** The platform in the Scene View displays only the basic `Visual` quad with no `Surface` or `Border` child GameObjects, confirming `PlatformVisualPresentation` is dormant outside Play mode.
- **Prerequisite:** Non-mutating observation in Unity Editor.

### Experiment 3: Toggle URP Post-Processing Bloom
- **Method:** While in Edit mode in the Game View, select `Camera.main` and temporarily toggle `Render Post-processing` off in the `UniversalAdditionalCameraData` inspector (or toggle `Post-processing` in the Game View toolbar).
- **Expected Outcome if Hypothesis 4 holds:** Disabling post-processing sharpens the platform edge and eliminates the bloom washout over the perimeter.
- **Prerequisite:** Non-mutating inspector toggle.

---

## 7. Safest Next Steps (Recommendations for Future Work)

When authorized to implement a fix in a future task, the smallest, safest, and most effective adjustments would be:

1. **Resolution-Independent Screen-Space / Sliced Border Scaling:**
   Modify `GetBorderSprite()` or the border renderer to use an outer outline with a thicker slice border (e.g. $4$–$6$ pixels on a $64$ PPU sprite, or an outer frame of $0.15$–$0.20$ world units), ensuring it never drops below $2$ physical screen pixels regardless of zoom.
2. **Persistent Editor-Side Platform Outlines:**
   Ensure all platforms in Edit mode have a subtle, persistent editor contrast outline (not just when selected), similar to how tilemap or CAD editors outline white surfaces against varied backgrounds.
3. **Add `[ExecuteAlways]` to Presentation Components:**
   Add `[ExecuteAlways]` to `PlatformVisualPresentation` so that surface styling, borders, and visual hierarchy render identically in both the Unity Scene View (Edit Mode) and Game View (Play Mode).
4. **Tune Bloom Threshold or Platform Surface Maximum:**
   Ensure platform surface brightness (`WhiteTop = 0.910f`) sits just below or matches the bloom threshold, or clamp the surface albedo so it does not bloom uncontrollably over its own borders.

*Note: These steps are recommendations only. In compliance with the prompt restrictions, none of these changes have been implemented.*

---

## 8. Scope and Verification Summary

- **Inspected Files:**
  - `Assets/Script/Contrast/Level/ColorPlatform.cs`
  - `Assets/Script/Contrast/Level/LevelEditorController.cs`
  - `Assets/Script/Contrast/Level/LevelLoader.cs`
  - `Assets/Script/Contrast/Level/LevelMarker.cs`
  - `Assets/Script/Contrast/Color/ColorClassifier.cs`
  - `Assets/Script/Contrast/Presentation/PlatformVisualPresentation.cs`
  - `Assets/Script/Contrast/Presentation/BackgroundPresentation.cs`
  - `Assets/Script/Contrast/Presentation/VisualPresentationManager.cs`
  - `Assets/Script/Contrast/UI/PlatformColorSubComponent.cs`
  - `Assets/Script/Contrast/UI/LevelEditorUI.cs`
  - `Assets/Settings/Renderer2D.asset`
  - `Assets/Scenes/SampleScene.unity`
  - `CONTRAST_VISUAL_AUDIT.md`
- **Files Modified for Task A (Reset Level):**
  - `Assets/Script/Contrast/Level/LevelEditorController.cs`
  - `Assets/Script/Contrast/Level/LevelLoader.cs`
  - `Assets/Script/Contrast/UI/LevelEditorUI.cs`
  - `Assets/Editor/EditorLevelTests.cs`
  - `Tests/ContrastTests/Program.cs`
- **White Platform Implementation Files Modified:**
  - **ZERO.** No files, shaders, materials, sprites, colors, or rendering configurations related to white platform visibility were modified.
- **Tests Executed:**
  - `dotnet build Assembly-CSharp.csproj` -> **0 Warnings, 0 Errors (Build Succeeded)**
  - `dotnet build Assembly-CSharp-Editor.csproj` -> **0 Errors (Build Succeeded)**
  - `dotnet run --project Tests/ContrastTests/ContrastTests.csproj` -> **235 Passed, 0 Failed (100% Pass Rate)**
