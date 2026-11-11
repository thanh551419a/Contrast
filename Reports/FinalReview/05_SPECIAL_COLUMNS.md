# 05 — SPECIAL COLUMNS GRAY-BLUE APPEARANCE (`Reports/FinalReview/05_SPECIAL_COLUMNS.md`)

## 1. HTML Reference (`CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html`)

In `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html` (`drawTerrainBlock(b)` and `fx` block factory):

- **Data representation:**
  - HTML `fx(x, y, w, h, label, extra)` creates structural blocks with `material: 'FIXED', structural: true, alwaysSolid: true`.
  - In Unity JSON (`L01.json`, `L02.json`, `L03.json`), these blocks are stored with `"rawGrayscaleColor": 255.0`, which `ColorClassifier.Classify(255f)` maps to `LogicalColor.Universal` (always solid regardless of Player phase).
- **HTML Visual Styling (`drawTerrainBlock` when `b.structural` is true):**
  - Vertical gradient fill:
    - Top stop (`0`): `#28323b` (`RGB(40, 50, 59)` / `(0.157, 0.196, 0.231)`)
    - Bottom stop (`1`): `#151c24` (`RGB(21, 28, 36)` / `(0.082, 0.110, 0.141)`)
  - Border stroke: `rgba(213, 225, 232, 0.55)` (`RGB(213, 225, 232)` / `(0.835, 0.882, 0.910, 0.55)`)
  - Structural diagonal hatch lines (`step = 18px`): `rgba(255, 255, 255, 0.055)`

---

## 2. Implementation (`PlatformVisualPresentation.cs`, `ColorClassifier.cs`, `PlatformColorSubComponent.cs`)

1. **`PlatformVisualPresentation.cs`:**
   - Updated `UniversalTop` from Gold/Amber `(0.961f, 0.620f, 0.043f)` to Gray-Blue `(0.157f, 0.196f, 0.231f, 1.00f)` (`#28323b`).
   - Updated `UniversalBottom` from `(0.851f, 0.467f, 0.024f)` to `(0.082f, 0.110f, 0.141f, 1.00f)` (`#151c24`).
   - Updated `UniversalBorder` to `(0.835f, 0.882f, 0.910f, 0.55f)` (`rgba(213, 225, 232, 0.55)`).
   - Updated `UniversalGlow` to `(0.525f, 0.635f, 0.745f, 0.16f)`.
   - Enhanced `GetUniversalSprite()` to render the subtle diagonal structural hatch lines (`(x + y) % 16 < 2`, `rgba(255, 255, 255, 0.055)`) matching HTML `drawTerrainBlock`.
   - **Newton Orbital Platform K Safeguard:** In `UpdateSurfaceStyle()`, `OrbitPlatform_K` (`OrbitPlatform_K_White`) retains its bright `White` visual styling (`GetWhiteSprite()`, `WhiteBorder`) while using `rawGrayscaleColor = 255f` (`LogicalColor.Universal`) for always-solid collision semantics.
2. **`ColorClassifier.cs` & `PlatformColorSubComponent.cs`:**
   - Updated `ColorClassifier.GetPlatformVisualColor(255f)` to return `new UnityEngine.Color(0.157f, 0.196f, 0.231f, 1f)` (`#28323b`).
   - Updated the Level Editor `ALL 255` preset swatch and button color in `PlatformColorSubComponent.cs` to Gray-Blue so editor previews match runtime visuals.
3. **Collision Semantics Preserved:**
   - `ColorClassifier.CanCollide(playerColor, LogicalColor.Universal)` remains `true` for all player phases (`White`, `Gray`, `Black`).

