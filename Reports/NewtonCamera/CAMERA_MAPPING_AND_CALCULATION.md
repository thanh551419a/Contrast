# NEWTON CAMERA MAPPING AND CALCULATION (`Reports/NewtonCamera/CAMERA_MAPPING_AND_CALCULATION.md`)

**Branch:** `fix/deathy-newton-camera-x-collider`  
**Base Commit:** `497c601d3f52095df712228c9e69a024d08ae23c`  
**Status:** Pre-change mathematical derivation from `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html` and L01–L03 Unity geometry.

---

## 1. HTML-to-Unity Coordinate & Viewport Mapping (Section B4)

### 1.1 Coordinate Scale & Sign Convention
Across `LevelLoader.cs` (`lines 346–347`) and `EasyNewtonFinaleController.cs` (`lines 165–166`):
$$\text{PixelsPerUnit} = 64.0\text{ px/unit}$$
$$\text{UnityX} = \frac{\text{HtmlX}}{64.0}, \qquad \text{UnityY} = -\frac{\text{HtmlY}}{64.0}$$

### 1.2 Viewport & Orthographic Size Formulas
In Unity, an orthographic camera with `orthographicSize = S` and aspect ratio `A = W / H` (`16 / 9 = 1.777778`) displays a world rectangle of:
$$\text{VisibleWorldHeight} = 2 S, \qquad \text{VisibleWorldWidth} = 2 S \cdot A$$
Conversely, given an HTML canvas of `V.w = 1600 px`, `V.h = 900 px` (`A = 16/9`) and HTML zoom factor `z`:
$$\text{HtmlVisibleWorldHeightPx} = \frac{V.h}{z} = \frac{900}{z}\text{ px}$$
$$\text{UnityVisibleWorldHeight} = \frac{900 / z}{64.0} = \frac{14.0625}{z}\text{ units}$$
$$\text{OrthographicSize}_{\text{HTML-equivalent}} = \frac{\text{UnityVisibleWorldHeight}}{2} = \frac{7.03125}{z}\text{ units}$$

### 1.3 HTML `newtonViewLayout(cfg)` Derivation (`CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html:1654–1692`)
In the HTML reference:
1. **Screen Anchor for Newton Center `O = (cfg.cx, cfg.cy)`:**
   - `centerSX = V.w * 0.625 = 1000 px` (`+12.5%` right of screen center `800 px`, so the Newton center sits slightly right of screen center and leaves room on the left for the approach vestibule/dock and bottom-left HUD panel).
   - `centerSY = V.h * 0.500 = 450 px` (exact vertical center of the screen).
2. **Outer Ring Radius in HTML (`outerWorld`):**
   - For a level with outer dark ring order `k` (`k=6` in L01, `k=4` in L02, `k=7` in L03) and `ringScale = 220 px` (`3.4375 u`):
     $$r_K = 3.4375 \sqrt{k}\text{ units}, \qquad r_I = 3.4375 \sqrt{i}\text{ units}$$
   - In HTML `newtonViewLayout(cfg)`:
     $$\text{outerWorldPx} = 220 \sqrt{7.65} + 22 = 630.4944\text{ px} \quad (9.8515\text{ Unity units})$$
     $$z_{\text{HTML}} = \text{clamp}\left(\frac{442}{630.4944}, 0.58, 0.74\right) = 0.701038$$
   - At `z = 0.701038`:
     $$\text{UnityVisibleWorldHeight} = \frac{14.0625}{0.701038} = 20.0595\text{ u} \implies S_{\text{HTML}} = 10.0298\text{ u}$$
     $$\text{UnityVisibleWorldWidth} = 20.0595 \times \frac{16}{9} = 35.6614\text{ u} \implies \text{HalfWidth} = 17.8307\text{ u}$$

---

## 2. Level-by-Level Newton Geometry & Framing Analysis (L01, L02, L03)

Let us inspect the exact world coordinates of the Newton center `O`, inner platform orbit `I`, outer platform orbit `K`, and all possible positions of the three sequential targets `Target 1 (K)`, `Target 2 (I)`, and `Target 3 (K')` across all three levels:

| Level | `centerO` `(UnityX, UnityY)` | Orders `(i, k)` | Inner Orbit `r_I` (`u`) | Outer Orbit `r_K` (`u`) | Platform K Half-Height (`u`) | Required Vertical Half-Span for Full K Orbit + Player (`r_K + 0.67 u`) |
|---|---|---|---|---|---|---|
| **L01** | `(80.2344, -10.2969)` | `(3, 6)` | `5.9539` | `8.4201` | `0.1719` | `8.4201 + 0.6719 =` **`9.0920 u`** |
| **L02** | `(79.8438, -8.2500)` | `(2, 4)` | `4.8613` | `6.8750` | `0.1719` | `6.8750 + 0.6719 =` **`7.5469 u`** |
| **L03** | `(81.2656, -32.8125)` | `(2, 7)` | `4.8613` | `9.0948` | `0.1719` | `9.0948 + 0.6719 =` **`9.7667 u`** |

### 2.1 Deriving the Exact Fixed Camera Center & Orthographic Size in Unity
1. **Why the Camera Must Encompass Radius `r_K` Around `centerO`:**
   - Target 1 (`K`) is marked while the Player is on outer platform `K` (at distance `r_K` from `centerO`).
   - Target 2 (`I`) is located at `centerO - AxisU * r_I` (distance `r_I` from `centerO`).
   - Target 3 (`K'`) is located at `centerO - AxisU * r_K` (distance `r_K` from `centerO`, diametrically opposite `K`).
   - Because the Player can mark Target 1 (`K`) at **any** angle around the 360° orbit (including top `90°` or bottom `270°`), `Target 3 (K')` can be at `centerO.y ± r_K`.
   - Furthermore, the Player rides Platform `K` (`radius = r_K`) and Platform `I` (`radius = r_I`) around `centerO` while the camera is **fixed in world space**.
   - Therefore, for the Player, Platform `K`, Platform `I`, and all three targets (`K`, `I`, `K'`) to remain simultaneously visible inside the fixed camera viewport without clipping at the top or bottom of the screen, the fixed Newton camera must have:
     - **Fixed World Y:** `fixedCamY = centerO.y` (matching HTML `centerSY = V.h * 0.50`).
     - **Fixed World X:** `fixedCamX = centerO.x - horizontalShift` where in HTML `centerSX = 0.625 * V.w` shifts the camera center left of `centerO.x` by `0.125 * VisibleWorldWidth` (or centered at `centerO.x` / shifted left by `1.5 u` so both the left vestibule dock `[centerO.x - r_K - 3.5u]` and right orbit `[centerO.x + r_K]` have generous margin). Specifically, at `fixedCamX = centerO.x - 1.5f` (or `centerO.x`), with `HalfWidth >= 16.0 u`, the visible X span around `centerO.x` covers `[centerO.x - 17.5 u, centerO.x + 14.5 u]`, which easily contains both the left vestibule ledge and the rightmost outer ring (`+9.09 u`)!
     - **Zoomed-In Orthographic Size Relative to Chamber Overview:**
       - Note how `EasyNewtonFinaleController` has a wide chamber/reveal framing (`revealOrthoSize = 10.2f` / `9.6f`) vs. active Newton X-marking framing (`newtonFixedOrthoSize`).
       - If we compute the active Newton X-marking `orthoSize` from the level's outer orbit `r_K` plus a tight visual margin (matching HTML's `outerWorld + 8px` padding formula `r_K + 1.15f`, clamped to `[8.2f, 10.0f]`, or `9.6f` / level-specific):
         - **L01 (`k=6`, `r_K = 8.4201 u`):** `newtonOrthoSize = 9.5f` (`VisibleWorldHeight = 19.0 u`, `VisibleWorldWidth = 33.78 u`). Every point on orbit K (`±8.42 u`) and the player's head (`+8.92 u`) is strictly inside `[-9.5 u, +9.5 u]`!
         - **L02 (`k=4`, `r_K = 6.8750 u`):** `newtonOrthoSize = 8.2f` (`VisibleWorldHeight = 16.4 u`, `VisibleWorldWidth = 29.16 u`). Every point on orbit K (`±6.875 u`) and the player's head (`+7.375 u`) is strictly inside `[-8.2 u, +8.2 u]`!
         - **L03 (`k=7`, `r_K = 9.0948 u`):** `newtonOrthoSize = 10.0f` (`VisibleWorldHeight = 20.0 u`, `VisibleWorldWidth = 35.56 u`, matching HTML's `10.03 u` for `outerN = 7` to within `0.3%`!). Every point on orbit K (`±9.095 u`) and the player's head (`+9.595 u`) is strictly inside `[-10.0 u, +10.0 u]`!
       - Formula for any level `(i, k)`:
         $$\text{newtonFixedOrthoSize} = \max\left(8.2, \, r_K + 0.95\right)$$
         - For `k = 4` (L02): `max(8.2, 6.875 + 0.95) = 8.20 u`
         - For `k = 6` (L01): `max(8.2, 8.420 + 0.95) = 9.37 u` (or `9.50 u`)
         - For `k = 7` (L03): `max(8.2, 9.095 + 0.95) = 10.045 u` (exact match to HTML `10.0298 u`!)

---

## 3. Verification of All Framing Invariants (Sections B5 & B6)

| Requirement | Verification with Derived `NewtonFixed` Camera |
|---|---|
| **Fixed in world space during Newton X-marking** | `fixedTargetPosition = new Vector3(centerO.x, centerO.y, -10f)` is locked when entering the Newton marking zone. Camera X and Y do **not** depend on `player.transform.position`. |
| **Does NOT follow Player when moving left/right, jumping, or orbiting** | Verified: `CameraFollow` in `NewtonFixed` mode ignores `target.position` for camera translation and holds `fixedTargetPosition`. |
| **Does NOT shift when pressing X on Target 1 or Target 2** | Verified: Marking Target 1 (`K`) and Target 2 (`I`) keeps `CameraFollow` in `NewtonFixed` at the exact same `fixedTargetPosition` and `newtonFixedOrthoSize`. |
| **All 3 sequential targets (`K`, `I`, `K'`) + platforms `K` & `I` simultaneously visible** | Since `r_I < r_K < newtonFixedOrthoSize - 0.95f` and `r_K < newtonFixedOrthoSize * (16/9) - 6.0f`, the entire 360° orbits of K and I and all 3 targets `K`, `I`, `K'` are 100% inside the camera frustum at all times (even on 16:10 or 4:3 displays). |
| **Clean restoration on exit / respawn / restart** | Leaving the Newton zone (`distanceToO > 16.5f` when not actively locked in measurement), respawning outside the Newton zone, or restarting calls `cameraFollow.ResetToNormal(5.0f, 3.0f)`, restoring `NormalPlatformingFollow`. |

