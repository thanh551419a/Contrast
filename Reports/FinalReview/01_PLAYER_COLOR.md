# 01 — PLAYER BLACK COLOR APPEARANCE (`Reports/FinalReview/01_PLAYER_COLOR.md`)

## 1. Reference Analysis (`CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html`)

In `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html` (`drawPlayer(dt)` and `updatePlayer(dt)`):

- **Optical Phase Equation:**
  $$I(\phi) = \cos^2\left(\frac{\phi}{2}\right), \quad \phi \in [0, 2\pi)$$
- **Phase Thresholds (`getStateForIntensity`):**
  - `BRIGHT` (`White`): $I \ge 0.78$
  - `GRAY` (`Gray`): $0.22 < I < 0.78$
  - `DARK` (`Black`): $I \le 0.22$
- **HTML Player Body & Stroke Colors:**
  - `BRIGHT` (`White`): `fillStyle = '#ffffff'` (`RGB(255, 255, 255)` / `(1.000, 1.000, 1.000)`), `strokeStyle = '#d9e3eb'`
  - `GRAY` (`Gray`): `fillStyle = '#858585'` (`RGB(133, 133, 133)` / `(0.522, 0.522, 0.522)`), `strokeStyle = '#cbd4dc'`
  - `DARK` (`Black`): `fillStyle = '#050505'` (`RGB(5, 5, 5)` / `(0.020, 0.020, 0.020)`), `strokeStyle = '#74818b'` (`RGB(116, 129, 139)` / `(0.455, 0.506, 0.545)`)

---

## 2. Root Cause of Gray Appearance in Unity

1. **Gray-tinted `DarkBody` constant:** `PlayerVisualPresentation.DarkBody` was set to `new Color(0.11f, 0.13f, 0.16f, 1f)` (`#1C2129`), and `ColorClassifier.ToValue(LogicalColor.Black)` returned `0.15f`.
2. **Baked gray highlight in procedural `GetBodySprite()`:** `GetBodySprite()` baked a bright inner core (`coreBoost` up to `+0.18f`) and a bright rim (`0.95f`) directly into the procedural texture pixels, preventing `SpriteRenderer.color` multiplication from darkening the sprite when `DarkBody` had low RGB values and making the rim invisible when `DarkBody` approached `(0.02, 0.02, 0.02)`.

---

## 3. Implementation (`PlayerVisualPresentation.cs`, `ColorClassifier.cs`, `PlayerController.cs`)

- Updated `PlayerVisualPresentation` colors to match HTML `drawPlayer`:
  - `BrightBody = new Color(1.000f, 1.000f, 1.000f, 1.00f)` (`#ffffff`)
  - `GrayBody = new Color(0.522f, 0.522f, 0.522f, 1.00f)` (`#858585`)
  - `DarkBody = new Color(0.020f, 0.020f, 0.020f, 1.00f)` (`#050505`)
  - `BrightStrokeRim = new Color(0.851f, 0.890f, 0.922f, 0.95f)` (`#d9e3eb`)
  - `GrayStrokeRim = new Color(0.796f, 0.831f, 0.863f, 0.92f)` (`#cbd4dc`)
  - `DarkStrokeRim = new Color(0.455f, 0.506f, 0.545f, 0.95f)` (`#74818b`)
- Separated the player disc into:
  1. **Unbaked pure-white interior disc (`GetBodySprite()`)** on `bodyRenderer`, so `bodyRenderer.color = DarkBody` renders at true `#050505` (`0.020, 0.020, 0.020`).
  2. **Dedicated `OpticalRim` child `SpriteRenderer` (`GetRimSprite()`)** tinted with `DarkStrokeRim` (`#74818b`), keeping the black Player silhouette crisp and readable against the dark `#070a0f` chamber background while remaining unmistakably black (`#050505`) inside.
- Updated `ColorClassifier.ToValue(LogicalColor.Black)` to `0.02f` and `LogicalColor.Gray` to `0.522f`.
- Updated `PlayerController.ApplyColorVisual()` to invoke `SyncVisualsNow()` on `PlayerVisualPresentation`.

---

## 4. Before / After RGB Comparison

| Phase State | HTML Reference (`fillStyle` / `strokeStyle`) | Unity Before (`bodyRenderer.color`) | Unity After (`bodyRenderer.color` + `rimRenderer.color`) |
| :--- | :--- | :--- | :--- |
| **White (`BRIGHT`)** | `#ffffff` (`1.000, 1.000, 1.000`) / `#d9e3eb` | `(0.97, 0.98, 1.00)` | Body `(1.000, 1.000, 1.000)` (`#ffffff`) + Rim `(0.851, 0.890, 0.922)` (`#d9e3eb`) |
| **Gray (`GRAY`)** | `#858585` (`0.522, 0.522, 0.522`) / `#cbd4dc` | `(0.53, 0.57, 0.62)` | Body `(0.522, 0.522, 0.522)` (`#858585`) + Rim `(0.796, 0.831, 0.863)` (`#cbd4dc`) |
| **Black (`DARK`)** | `#050505` (`0.020, 0.020, 0.020`) / `#74818b` | `(0.11, 0.13, 0.16)` + baked core | Body `(0.020, 0.020, 0.020)` (`#050505`) + Rim `(0.455, 0.506, 0.545)` (`#74818b`) |

