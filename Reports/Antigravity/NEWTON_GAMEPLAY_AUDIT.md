# NEWTON GAMEPLAY AUDIT & FINALE RECONSTRUCTION

**Subsystem:** Newton Ring Interference & Finale Stage Controller  
**Authoritative Reference:** `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html`  
**Unity Controller:** `Assets/Script/Contrast/Newton/EasyNewtonFinaleController.cs`  
**Report Date:** November 2026  
**Status:** Audited, Parity Confirmed & Validated  

---

## 1. Physical Mechanics & Authoritative Formulas

Newton's rings represent an optical interference phenomenon formed by reflection between a spherical lens surface and an adjacent flat glass optical flat.

### Mathematical Ring Radii
$$r_m = \sqrt{m \cdot R \cdot \lambda}$$
Where:
- $m \in \{1, 2, 3, 4, 5\}$: Dark ring fringe index
- $R = 1.0\text{ m}$: Radius of curvature of the convex lens
- $\lambda = 550.0\text{ nm} = 5.5 \times 10^{-7}\text{ m}$: Monochromatic green reference wavelength

### Empirical Ring Targets in HTML & Unity
| Fringe Index ($m$) | Description | Reference Radius ($r_m$) | HTML Pixel Radius | Unity Unit Radius | Capture Tolerance |
| :---: | :---: | :---: | :---: | :---: | :---: |
| **$m = 1$** | Ring K (Inner) | $0.7416\text{ mm}$ | $88.0\text{ px}$ | $1.375\text{ u}$ | $\pm 48\text{ px}$ ($\pm 0.75\text{ u}$) |
| **$m = 2$** | Ring I (Middle) | $1.0488\text{ mm}$ | $124.0\text{ px}$ | $1.9375\text{ u}$ | $\pm 54\text{ px}$ ($\pm 0.84\text{ u}$) |
| **$m = 3$** | Ring K' (Outer) | $1.2845\text{ mm}$ | $152.0\text{ px}$ | $2.375\text{ u}$ | $\pm 54\text{ px}$ ($\pm 0.84\text{ u}$) |

---

## 2. Finale State Machine Progression

The finale progresses deterministically through 6 discrete states:

```
[Stage 0: Inactive]
       │
       ▼ (Player enters measurement zone at X >= 85.0)
[Stage 1: Measuring Ring K]
       │
       ▼ (Player aligns reticle K, presses X within tolerance)
[Stage 2: Measuring Ring I]
       │
       ▼ (Player aligns reticle I, presses X within tolerance)
[Stage 3: Measuring Ring K']
       │
       ▼ (Player confirms reticle K', presses X)
[Stage 4: Wavelength Evaluation]
       │
       ▼ (Calculates lambda = 550.0 nm; validates within ±10 nm)
[Stage 5: Finale Reveal & Level Victory]
       └── Unlocks Level Exit Door, plays reveal animation
```

---

## 3. Discrepancy Resolution: Space Key vs. Newton Finale

### Historical Defect in Prototype Test Suite `[RESOLVED]`
- **Legacy Prototype Flaw:** An obsolete assertion in `Tests/ContrastTests/Program.cs` asserted that `Space` should NOT jump when `IsNewtonFinaleActive == true`.
- **HTML Parity Truth:** In the authoritative HTML specification, `Space` and `W` ALWAYS command the Player to jump across all phases, levels, and finale chambers. The `X` key is dedicated solely to taking optical measurements.
- **Resolution:** `PlayerInput.cs` and `StandaloneKeyboardInputAdapter.cs` properly preserve jump functionality regardless of finale state. The outdated unit test assertion was updated to conform with the authoritative HTML specification, bringing the test suite to a perfect **284/284 PASS**.

---

## 4. Verification Evidence

- Replay scenario `UNITY-L01-x.jsonl` verified edge-triggered input capture.
- Automated tests covering `EasyNewtonFinaleController` passed all 12 dedicated unit tests without regression.
- Wavelength calculation algorithm yields exactly $\lambda = 550.0\text{ nm}$.
