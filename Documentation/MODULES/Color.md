# Module: Color

> **Namespace:** `Contrast.Color`  
> **Subsystem:** Color Classification & Chromatic Mechanics

---

## 1. Responsibility
The **Color** module establishes the core mathematical rules governing the game's color-contrast mechanics. It maps raw continuous grayscale values ($0.0 \dots 255.0$) into discrete logical buckets (`White`, `Gray`, `Black`, `Universal`) and provides authoritative visual color palettes for sprites and UI elements.

---

## 2. Main Files & Classes

| File Path | Primary Types | Responsibility |
|---|---|---|
| [`Assets/Script/Contrast/Color/LogicalColor.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/LogicalColor.cs) | `LogicalColor` (`enum`) | Discrete color classifications: `White = 0`, `Gray = 1`, `Black = 2`, `Universal = 3`. |
| [`Assets/Script/Contrast/Color/ColorClassifier.cs`](file:///y:/Contrast/My%20project/Assets/Script/Contrast/Color/ColorClassifier.cs) | `ColorClassifier` (`static class`) | Classification thresholds, default values, and visual color mappings. |

---

## 3. Mathematical Classification Rules

| Logical Color | Raw Range | Nominal Center | Visual Color / Tint | Collision Behavior |
|---|:---:|:---:|---|---|
| **White** | $0.0 \le v \le 84.5$ | $0.0$ | Pure White `RGB(1.0, 1.0, 1.0)` | Solid to Gray, Black, Universal; transparent to White. |
| **Gray** | $84.5 < v \le 169.5$ | $127.0$ | Medium Gray `RGB(0.55, 0.55, 0.55)` | Solid to White, Black, Universal; transparent to Gray. |
| **Black** | $169.5 < v < 254.5$ | $212.0$ | Dark Charcoal `RGB(0.12, 0.12, 0.12)` | Solid to White, Gray, Universal; transparent to Black. |
| **Universal (ALL)**| $v \ge 254.5$ | $255.0$ | Gold / Amber `RGB(0.96, 0.78, 0.22)` | **Always solid** to all player colors. |

---

## 4. Key Methods

- **`ColorClassifier.Classify(float raw)`**: Returns `LogicalColor`.
- **`ColorClassifier.DefaultValue(LogicalColor c)`**: Returns center float ($0f, 127f, 212f, 255f$).
- **`ColorClassifier.ToValue(LogicalColor c)`**: Returns float brightness for sprites ($1.0, 0.5, 0.1, 0.85$).
- **`ColorClassifier.GetPlatformVisualColor(float raw)`**: Returns `UnityEngine.Color`. Universal platforms receive distinct gold/amber tint so designers and players immediately identify universal solidity.
- **`ColorClassifier.GetTypeName(LogicalColor c)`**: Human-readable name for editor UI dropdowns.

---

## 5. Dependencies
- **Downstream Consumers:**
  - `ColorPlatform` (initializes `Logical` and visual color).
  - `PlayerController` (manages `CurrentColor` and player sprite tint).
  - `PlayerCollisionResolver` and `PlayerOverlapResolver` (evaluates blocking condition).
  - `LevelMarker` (renders marker tint based on color).
  - `PlatformColorSubComponent` (renders inspector dropdowns and swatches).

---

## 6. State Ownership
- Pure stateless static module; owns constants and classification formulas.

---

## 7. Important Invariants
- `Universal` ($v \ge 254.5$) must ALWAYS block the player regardless of active player color.
- For standard colors, collision is blocked if and only if `platform.Logical != playerColor`.

---

## 8. Related Modules & Documentation
- [Player Module](file:///y:/Contrast/My%20project/Documentation/MODULES/Player.md)
- [Movement Module](file:///y:/Contrast/My%20project/Documentation/MODULES/Movement.md)
- [LevelRuntime Module](file:///y:/Contrast/My%20project/Documentation/MODULES/LevelRuntime.md)
