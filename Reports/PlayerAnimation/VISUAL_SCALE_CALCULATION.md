# PLAYER VISUAL SCALE & FEET ALIGNMENT CALCULATION REPORT

## 1. Authoritative Gameplay Collider Measurements (Unchanged)

From `PlayerController.cs` and `L01.json` / `L02.json` / `L03.json`:
- **`ColliderWidth` (`playerSize.x`)**: `0.5937500` world units (`38 px / 64 px/unit`)
- **`ColliderHeight` (`playerSize.y`)**: `1.0000000` world units (`64 px / 64 px/unit`)
- **`ColliderOffset` (`playerOffset`)**: `(0.0000000, 0.0000000)` world units
- **Player Root `transform.localScale`**: `(0.3000000, 0.3000000, 1.0000000)`
- **`ColliderCenter`**: `(transform.position.x, transform.position.y)`
- **`GroundContactFeetY`**: `transform.position.y - 0.5000000` world units (`transform.position.y - playerSize.y * 0.5f`)

---

## 2. Measured Non-Transparent Character Bounds Across All 213 Frames

All 213 frames across `Idle.png` (`120` frames), `Run.png` (`32` frames), and `Jump.png` (`61` frames) were scanned pixel-by-pixel at `PixelsPerUnit = 128.0`:

| Animation Clip | Frame Count | Non-Transparent Union BBox (Top-Down `0..255`) | Max Frame Width (px / units) | Max Frame Height (px / units) | Union Envelope (px / units) |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **`Idle`** | `120` | `(76, 32) – (179, 223)` | `104 px` (`0.81250 u`) | `192 px` (`1.50000 u`) | `104 × 192 px` (`0.81250 × 1.50000 u`) |
| **`Run`** | `32` | `(51, 25) – (199, 223)` | `134 px` (`1.04688 u`) | `199 px` (`1.55469 u`) | `149 × 199 px` (`1.16406 × 1.55469 u`) |
| **`Jump`** | `61` | `(45, 19) – (212, 223)` | `168 px` (`1.31250 u`) | `199 px` (`1.55469 u`) | `168 × 205 px` (`1.31250 × 1.60156 u`) |
| **Complete Set (All 213 Frames)** | **`213`** | **`(44, 19) – (211, 223)` (symmetric)** | **`168 px` (`1.31250 u`)** | **`199 px` (`1.55469 u`)** | **`168 × 205 px` (`1.31250 × 1.6015625 u`)** |

### Recorded Input Metrics
```text
MaxAnimatedVisualWidth  = 168 px / 128 PPU = 1.3125000 units
MaxAnimatedVisualHeight = 205 px / 128 PPU = 1.6015625 units

ColliderWidth           = 0.5937500 units
ColliderHeight          = 1.0000000 units

VisualPivot             = (0.5000000, 0.0000000) on cropped 168×205 rect
                          [identically (0.5, 0.125) = pixel (128, 32) on 256×256 cell]
ColliderCenter          = (transform.position.x, transform.position.y)
FeetOffset (World)      = (0.0000000, -0.5000000) units
FeetOffset (Local)      = (0.0000000, -1.6666667) units [compensated for parent scale 0.3]
```

---

## 3. Uniform Visual Scale & Aspect Ratio Calculation

Using the mandatory uniform scale formula:
```csharp
float widthRatio  = ColliderWidth  / MaxAnimatedVisualWidth;   // 0.5937500 / 1.3125000 = 0.45238096f
float heightRatio = ColliderHeight / MaxAnimatedVisualHeight;  // 1.0000000 / 1.6015625 = 0.62439024f
float uniformWorldScale = Mathf.Min(widthRatio, heightRatio);  // 0.45238096f
```

### Parent Scale Compensation
Because `Player.transform.localScale = (0.3, 0.3, 1.0)`:
```csharp
float localScaleX = uniformWorldScale / 0.3f; // 0.45238096f / 0.3f = 1.5079365f
float localScaleY = uniformWorldScale / 0.3f; // 0.45238096f / 0.3f = 1.5079365f
visualRoot.localScale = new Vector3(1.5079365f, 1.5079365f, 1.0f);
```

### Resulting World-Space Rendered Dimensions
- **Maximum Animated Width (`Jump` outstretched pose)**:
  `1.3125000 * 0.45238096 = 0.5937500 units` (**100.00% of `ColliderWidth = 0.5937500 units`**)
- **Maximum Animated Height (`Jump` stretch pose)**:
  `1.6015625 * 0.45238096 = 0.7245210 units` (**72.45% of `ColliderHeight = 1.0000000 units`**)
- **Aspect Ratio Tradeoff Documentation**:
  The character's maximum animated envelope has aspect ratio `W / H = 168 / 205 = 0.8195` (due to wide arm/leg swing during `Jump` and `Run`), whereas the collider has aspect ratio `W / H = 0.59375 / 1.0 = 0.59375`. By choosing `uniformWorldScale = Mathf.Min(widthRatio, heightRatio) = 0.45238096f`, the character's original `1:1` aspect ratio is strictly preserved without horizontal or vertical distortion, and the widest animated pose fits 100% of the collider width without overflowing.
- **Feet Alignment Verification**:
  - Bottom of cropped sprite is at `y_bottom = 32 px` (`VisualPivot.y = 0.0f`).
  - `visualRoot.position.y = transform.position.y - 0.5000000 units = ColliderBounds.Min.y`.
  - Resulting feet alignment error: **`0.0000000 units`** (neither floating nor sinking into the platform).

