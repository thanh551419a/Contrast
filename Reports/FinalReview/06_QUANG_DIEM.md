# 06 — QUANG ĐIỂM (COLLECTIBLE) VISUAL PRESENTATION (`Reports/FinalReview/06_QUANG_DIEM.md`)

## 1. HTML Quang Điểm Reference (`CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html`)

In `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html` (`drawCollectible(c)`, `updateCollectibles(dt)`, and HUD):

- **Authored Level Collectibles:**
  - `L01`: 2 Quang Điểm (`c1`, `c2`)
  - `L02`: 2 Quang Điểm (`c1`, `c2`)
  - `L03`: 3 Quang Điểm (`c1`, `c2`, `c3`)
- **HTML Visuals (`drawCollectible`):**
  - Radial golden glow (`radius = 22px`): `rgba(255, 221, 148, 0.95)` $\to$ `rgba(255, 196, 94, 0.28)` $\to$ `rgba(255, 196, 94, 0)`
  - 4-pointed star (`8` alternating outer/inner vertices, `r1 = 10px`, `r2 = 4.5px`):
    - Fill: `#ffdfa0` (`RGB(255, 223, 160)` / `(1.000, 0.875, 0.627)`)
    - Stroke: `#ffb74d` (`RGB(255, 183, 77)` / `(1.000, 0.718, 0.302)`, `lineWidth = 1.4`)
  - Inner highlight dot (`r = 2.2px`): `#fffef1` (`RGB(255, 254, 241)` / `(1.000, 0.996, 0.945)`)
  - Idle animation: vertical bobbing `bob = sin(time * 3.2 + phase) * 5px` (`±0.078 units`) and gentle rotation `rot = sin(time * 1.7 + phase) * 0.35 rad` (`±20.05 deg`).
- **Collection Feedback (`updateCollectibles`):**
  - Trigger distance: `hypot(px - c.x, py - c.y) < player.r + c.r + 4`
  - Burst effect: 18 golden particles (`rgba(255, 217, 132, alpha)`) radiating outward over `0.42s - 0.72s`.
  - HUD Counter: Top-right `Quang điểm: collected / total`.

---

## 2. Implementation (`LevelMarker.cs`, `LevelMarkerVisualPresentation.cs`, `LevelLoader.cs`, `PhaseTunerUI.cs`)

1. **`QuangDiemItem` Runtime Component (`Assets/Script/Contrast/Level/LevelMarker.cs`):**
   - Tracks `Id`, `Index`, `Position`, `Radius`, `IsCollected`, and static `All`, `TotalCount`, and `CollectedCount`.
   - `Collect()` marks the item collected, disables its trigger collider, and triggers the particle burst on `QuangDiemVisualPresentation`.
2. **`QuangDiemVisualPresentation` (`Assets/Script/Contrast/Presentation/LevelMarkerVisualPresentation.cs`):**
   - Procedurally generates the 4-pointed golden star sprite (`GetStarSprite()`, `#ffdfa0` body + `#ffb74d` border + `#fffef1` center highlight) and radial golden aura (`GetRadialGlowSprite()`).
   - Animates vertical bobbing (`sin(t * 3.2 + phase) * 0.078125f`), gentle rotation (`sin(t * 1.7 + phase) * 20.05f` deg), and aura breathing in `LateUpdate()`.
   - Spawns 18 golden burst particles on `PlayCollectBurst()` when collected and hides the star/glow cleanly.
3. **`LevelLoader.cs` Integration:**
   - `BuildRuntimeLevel()` instantiates `QuangDiemItem` objects from `data.collectibles` with `CircleCollider2D (isTrigger = true)` and `QuangDiemVisualPresentation`.
   - `ProcessPlayerInteractions()` checks overlap between the Player and uncollected `QuangDiemItem` instances and calls `item.Collect()`.
   - `ClearRuntimeLevel()` destroys all `QuangDiemItem` objects and clears `QuangDiemItem.All` on level unload/restart.
4. **Top-Right HUD Counter (`Assets/Script/Contrast/Presentation/PhaseTunerUI.cs`):**
   - Added `QuangDiemHUDCard` (`✦  QUANG ĐIỂM   collected / total`) in `PhaseTunerCanvas` anchored at top-right `(-24f, -96f)` below `GameControlUI`.

