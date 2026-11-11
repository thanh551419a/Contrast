# HTML NEWTON CAMERA & VIEWPORT ANALYSIS (`Reports/NewtonCamera/HTML_CAMERA_ANALYSIS.md`)

**Reference File Inspected:** `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html`  
**Branch:** `fix/deathy-newton-camera-x-collider`  
**Audit Status:** Read-only HTML analysis completed **before** selecting or modifying any Unity camera parameters.

---

## 1. Authoritative HTML Reference Summary Block (Section B3)

```text
HTML_REFERENCE:
- File: CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html
- Functions / lines:
  - Canvas & logical viewport declaration: lines 22, 38 (`<canvas id="game" width="1600" height="900">`, `const V = { w: 1600, h: 900 }`)
  - Responsive 16:9 CSS display sizing: lines 14–20 (`width: min(100vw, calc(100vh * 16 / 9)); height: min(100vh, calc(100vw * 9 / 16))`)
  - World & camera state initialization: lines 67–68 (`const WORLD = { w: 5600, h: 1200, deathY: 1080 }; const camera = { x: 0, y: 0, zoom: 1 };`)
  - Free-world Newton proximity check: lines 381–389 (`editorNearNewton()`: `Math.hypot(px - cfg.cx, py - cfg.cy) < cfg.ringScale * 4.6`)
  - Newton framing activation check: lines 1644–1652 (`isNewtonFramingActive()`)
  - Newton view layout & zoom calculation: lines 1654–1683 (`newtonViewLayout(cfg)`)
  - World point to camera top-left mapping: lines 1685–1692 (`cameraForWorldPoint(worldX, worldY, screenX, screenY, zoom, freeBounds = false)`)
  - Frame camera update & fixed Newton camera target: lines 1784–1824 (`update(dt)` camera block)
  - World-to-screen rendering transform: lines 1838–1843 (`draw()` canvas transform)
- Canvas / viewport size:
  - Logical canvas size: 1600 x 900 pixels (16:9 aspect ratio = 1.777778)
- Normal camera behavior:
  - `camera.zoom` lerps to `1.0` (`camera.zoom += (1 - camera.zoom) * Math.min(1, dt * 4.2)` at line 1811).
  - Visible world dimensions at `zoom = 1.0`: `1600 x 900` HTML pixels = `25.0 x 14.0625` Unity world units (`64 px = 1 unit`, equivalent to `orthographicSize = 7.03125` in Unity).
  - Follows the Player horizontally and vertically with look-ahead offset: `desiredX = player.x - V.w * 0.34` (places player at 34% screen width from left) and `desiredY = player.y - V.h * 0.54` (places player at 54% screen height from top), smoothed with `Math.min(1, dt * 6)` (`lines 1806–1810`).
- Newton camera trigger:
  - Triggered when `inMeasureZone = isNewtonFramingActive()` (`line 1644`) is `true`:
    - In L01/L02/L03 (`__CE_FREE_WORLD === true`): `editorNearNewton()` (`line 381`), which returns `true` when Euclidean distance from Player center `(px, py)` to Newton center `(cfg.cx, cfg.cy)` is `< cfg.ringScale * 4.6 = 220 * 4.6 = 1012 px` (`15.8125` Unity world units).
    - Also remains active if `newton.DockActive && !newton.solved` (`line 1649`).
- Newton camera target position:
  - Fixed in world space! In `update(dt)` (`lines 1785–1790`), when `inMeasureZone` is true:
    `const layout = newtonViewLayout(cfg);`
    `const targetCam = cameraForWorldPoint(cfg.cx, cfg.cy, layout.centerSX, layout.centerSY, layout.zoom, true);`
  - Notice `targetCam` depends ONLY on `(cfg.cx, cfg.cy)` and `layout` — it does NOT depend on `player.x` or `player.y`.
  - `layout.centerSX = V.w * 0.625 = 1000 px`, `layout.centerSY = V.h * 0.50 = 450 px`.
  - Using the HTML world-to-screen transform `screenX = (x - cam.x) * zoom + V.w * (1 - zoom) / 2`, the world coordinate at the center of the 1600x900 viewport `(screenX = 800, screenY = 450)` is:
    `worldCenterX = cfg.cx - (layout.centerSX - V.w * 0.5) / layout.zoom = cfg.cx - 200 / 0.701038 = cfg.cx - 285.291 px` (`centerO.x - 4.4577` Unity units)
    `worldCenterY = cfg.cy - (layout.centerSY - V.h * 0.5) / layout.zoom = cfg.cy` (`centerO.y` Unity units)
- Newton camera zoom / scale:
  - `outerWorld = darkRingRadiusWorld(cfg.outerN + 0.65, cfg) + 22 = 220 * sqrt(7.65) + 22 = 630.4944 px` (`9.8515` Unity units).
  - `maxRadius = 442 px` (`min(992, 592, 442, 442, 526.97)`).
  - `layout.zoom = clamp(442 / 630.4944, 0.58, 0.74) = 0.701038`.
  - Smooth transition speed: `camLerp = Math.min(1, dt * 3.2)` for position and `Math.min(1, dt * 3.4)` for zoom (`lines 1791–1793`).
- Visible world width and height during Newton mode:
  - `VisibleWorldWidth = V.w / layout.zoom = 1600 / 0.701038 = 2282.33 px = 35.6614 Unity units`
  - `VisibleWorldHeight = V.h / layout.zoom = 900 / 0.701038 = 1283.81 px = 20.0595 Unity units` (half-height = `10.0298 Unity units`).
- How K, I, K' and the Newton rings fit inside the frame:
  - At `zoom = 0.701038` (half-height `10.03 u`), the full outer Newton ring `outerWorld = 9.8515 u` fits vertically inside `[-10.03 u, +10.03 u]` around `centerO.y` with `8 px` (`0.125 u`) screen margin on top and bottom, while shifting the camera center `4.458 u` left of `centerO.x` leaves `centerO.x` at `62.5%` screen width so the approach vestibule/ledge on the left and the full right-hand outer ring (`+9.85 u`) both fit with zero clipping.
```

---

## 2. Detailed Answers to All 18 Audit Questions (Section B2)

### Q1–Q4: Canvas & Viewport Dimensions
1. **Canvas backing width & height:** `1600 x 900` pixels (`<canvas id="game" width="1600" height="900"></canvas>` at `line 22`, `const V = { w: 1600, h: 900 }` at `line 38`).
2. **CSS display size / aspect ratio:** `16:9` aspect ratio (`1.777778`), scaled via CSS `width: min(100vw, calc(100vh * 16 / 9)); height: min(100vh, calc(100vw * 9 / 16));` (`lines 16–17`).
3. **World coordinate system:** Origin `(0, 0)` at top-left; `+X` points right, `+Y` points **downward** (`y` increases downward). Unity converts via `UnityX = HtmlX / 64.0f`, `UnityY = -HtmlY / 64.0f` (`1 Unity unit = 64 HTML pixels`).
4. **World-to-screen transform (`lines 1839–1843`):**
   ```javascript
   const z = camera.zoom || 1;
   ctx.save();
   ctx.translate(V.w * (1 - z) * 0.5, V.h * (1 - z) * 0.5);
   ctx.scale(z, z);
   ctx.translate(-camera.x, -camera.y);
   ```
   Therefore, any world point `(x, y)` maps to screen pixel `(screenX, screenY)`:
   $$\text{screenX} = (x - \text{camera.x})\cdot z + \frac{V.w(1 - z)}{2}, \qquad \text{screenY} = (y - \text{camera.y})\cdot z + \frac{V.h(1 - z)}{2}$$

### Q5–Q12: Normal vs. Newton Camera Behavior in HTML
5. **How normal camera follow works (`lines 1806–1824`):**
   - Tracks `player.x` and `player.y`:
     ```javascript
     const desiredX = player.x - V.w * 0.34;
     const desiredY = player.y - V.h * 0.54;
     camera.x += (desiredX - camera.x) * Math.min(1, dt * 6);
     camera.y += (desiredY - camera.y) * Math.min(1, dt * 6);
     camera.zoom += (1 - camera.zoom) * Math.min(1, dt * 4.2);
     ```
6. **How Newton camera mode is triggered (`lines 381–389, 1644–1652`):**
   - `isNewtonFramingActive()` returns `true` when `editorNearNewton()` is `true` (`Math.hypot(px - cfg.cx, py - cfg.cy) < cfg.ringScale * 4.6`, i.e., `1012 px = 15.8125 u` from `centerO`), OR when `newton.DockActive && !newton.solved`.
7. **Whether the Newton camera is fixed or player-following (`lines 1785–1793`):**
   - **Strictly FIXED in world space!**
   - Look at `lines 1785–1793`:
     ```javascript
     if (inMeasureZone) {
       const cfg = currentNewtonCfg();
       const layout = newtonViewLayout(cfg);
       const targetCam = cameraForWorldPoint(cfg.cx, cfg.cy, layout.centerSX, layout.centerSY, layout.zoom, true);
       const camLerp = Math.min(1, dt * 3.2);
       camera.x += (targetCam.x - camera.x) * camLerp;
       camera.y += (targetCam.y - camera.y) * camLerp;
       camera.zoom += (layout.zoom - camera.zoom) * Math.min(1, dt * 3.4);
     ```
   - `targetCam.x`, `targetCam.y`, and `layout.zoom` are functions **solely of `(cfg.cx, cfg.cy)` and `cfg`**, completely independent of `player.x` and `player.y`. Once in the Newton zone, moving the player left/right, riding orbit K/I, or jumping **does not move `targetCam` at all**.
8. **Exact world coordinates / anchor point of the Newton camera:**
   - Anchors the Newton center `(cfg.cx, cfg.cy)` to screen pixel `(centerSX, centerSY) = (1000, 450)` (or `(800, 450)` when centered on the rings).
9. **Whether zoom/scale factor is applied:**
   - Yes: `layout.zoom = 0.701038` (`newtonViewLayout` at `lines 1654–1683`), with a reveal zoom of `layout.zoom + 0.03 = 0.731038` when `newton.solved` is true (`lines 1794–1804`).
10. **Exact visible world width and height during Newton mode:**
    - In HTML (`newtonViewLayout`): `VisibleWorldHeight = 900 / 0.701038 = 1283.81 px = 20.06 u` (`orthoSize = 10.03 u`), `VisibleWorldWidth = 1600 / 0.701038 = 2282.33 px = 35.66 u`.
    - For each specific level's outer orbit `k` (`k=6` in L01, `k=4` in L02, `k=7` in L03), the active outer platform radius is `r_K = 3.4375 * sqrt(k)`:
      - **L01 (`k=6`):** `r_K = 8.4201 u`
      - **L02 (`k=4`):** `r_K = 6.8750 u`
      - **L03 (`k=7`):** `r_K = 9.0948 u`
11. **Whether camera transitions instantly or smoothly:**
    - Smoothly interpolates toward the fixed target (`dt * 3.2` for position, `dt * 3.4` for zoom), and once converged on the fixed target, remains completely stationary while the Player moves, jumps, orbits, and marks K, I, and K'.
12. **How and when the camera exits Newton mode:**
    - When `inMeasureZone` becomes `false` (e.g., Player moves outside `15.8125 u` of `centerO`, or respawns at a checkpoint outside the Newton region, or level restarts), the `else` branch (`lines 1805–1812`) smoothly returns `camera.zoom` to `1.0` and resumes normal player follow.

### Q13–Q18: Framing of Newton Rings, Platforms K & I, and Targets 1, 2, 3
13. **Where Newton rings are located relative to the fixed viewport:**
    - Vertically centered (`centerSY = V.h * 0.5 = 450 px`, i.e., `camY = centerO.y`), and horizontally positioned so both the left vestibule/dock and the full `360°` circular orbits of K and I are inside the 16:9 frame.
14. **Where moving platforms K and I travel relative to the fixed viewport:**
    - Platform K orbits at radius `r_K = 3.4375 * sqrt(k)` (`6.875 u` to `9.095 u`) around `centerO`.
    - Platform I orbits at radius `r_I = 3.4375 * sqrt(i)` (`4.861 u` to `5.954 u`) around `centerO`.
    - Every point on both circular orbits `(centerO.x ± r_K, centerO.y ± r_K)` lies strictly inside the fixed Newton camera viewport.
15. **Where the three X-marking points (`K`, `I`, `K'`) are located relative to the fixed viewport:**
    - Target 1 (`K`) is on the outer ring of radius `r_K`.
    - Target 2 (`I`) is at `centerO - AxisU * r_I` on the inner ring of radius `r_I`.
    - Target 3 (`K'`) is at `centerO - AxisU * r_K` on the outer ring of radius `r_K` diametrically opposite `K`.
    - Because the entire circle of radius `r_K` around `centerO` is inside the fixed viewport, all three points (`K`, `I`, `K'`) are guaranteed to be visible on screen for **any** capture angle `AxisU`!
16. **Are all three X-marking points visible simultaneously inside the fixed Newton camera view?**
    - **Yes!** Regardless of what angle on orbit `K` the player marks Target 1 (`K`), both Target 2 (`I = centerO - AxisU * r_I`) and Target 3 (`K' = centerO - AxisU * r_K`) lie within radius `r_K` of `centerO`, which is completely contained within the fixed camera viewport.
17. **Does the camera move when the Player moves left/right or jumps inside the Newton area?**
    - **No.** In HTML (`lines 1785–1790`), `targetCam` is computed purely from `(cfg.cx, cfg.cy)` and never references `player.x` or `player.y`.
18. **Does the camera move when the Player marks point 1, point 2, or point 3?**
    - Marking point 1 (`K`) and point 2 (`I`) keeps `targetCam` fixed at the exact same world position and zoom.
    - Upon marking point 3 (`K'`), `newton.solved` becomes `true`, triggering the reveal sequence (`lines 1794–1804`).

