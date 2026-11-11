# 03 — PHASE SLIDER DIVIDERS (`Reports/FinalReview/03_PHASE_SLIDER.md`)

## 1. HTML Phase Strip & Divider Reference (`CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html`)

In `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html` (`drawOmbreBar()` and `stripColorForPhase(p)`):

- **Cyclic Phase Domain:** $\phi \in [0, 2\pi)$, normalized $t = \frac{\phi}{2\pi} \in [0, 1)$.
- **Intensity Law:** $I(\phi) = \cos^2(\phi / 2)$.
- **Phase Thresholds (`CFG.thresholds`):**
  - `bright = 0.78` ($I \ge 0.78 \iff \text{BRIGHT / White}$)
  - `dark = 0.22` ($I \le 0.22 \iff \text{DARK / Black}$)
  - $0.22 < I < 0.78 \iff \text{GRAY / Gray}$
- **4 Transition Boundary Positions on $[0, 1]$:**
  1. $t_1 = \frac{\arccos(\sqrt{0.78})}{\pi} \approx 0.155401$ (`White -> Gray`)
  2. $t_2 = \frac{\arccos(\sqrt{0.22})}{\pi} \approx 0.344599$ (`Gray -> Black`)
  3. $t_3 = 1 - t_2 \approx 0.655401$ (`Black -> Gray`)
  4. $t_4 = 1 - t_1 \approx 0.844599$ (`Gray -> White`)
- **HTML Divider Rendering (`drawOmbreBar`):**
  ```javascript
  const t1 = Math.acos(Math.sqrt(CFG.thresholds.bright)) / Math.PI;
  const t2 = Math.acos(Math.sqrt(CFG.thresholds.dark)) / Math.PI;
  const marks = [t1, t2, 1 - t2, 1 - t1];
  ctx.strokeStyle = 'rgba(255,255,255,0.22)';
  ctx.lineWidth = 1;
  for (const m of marks) {
    const mx = x + w * m;
    ctx.beginPath();
    ctx.moveTo(mx, y - 3);
    ctx.lineTo(mx, y + h + 3);
    ctx.stroke();
  }
  ```

---

## 2. Implementation in `Assets/Script/Contrast/Presentation/PhaseTunerUI.cs`

1. **Exact Strip Gradient (`EvaluateHtmlStripColor`):**
   - Updated `GenerateOmbreSprite()` in `PhaseTunerUI.cs` to sample `EvaluateHtmlStripColor(localPhase)` matching HTML's `stripColorForPhase(p)` across all 256 horizontal texels (`White #ffffff` $\to$ `Gray #858585` $\to$ `Black #050505` $\to$ `Gray #858585` $\to$ `White #ffffff`).
2. **Outer Strip Border:**
   - Added a `UnityEngine.UI.Outline` (`rgba(235, 241, 244, 0.22)`) around `OmbreStrip` matching HTML `ctx.strokeStyle = 'rgba(235,241,244,0.18)'`.
3. **4 Phase Boundary Dividers (`PhaseDivider_0` .. `PhaseDivider_3`):**
   - Created 4 crisp UI `Image` vertical separator bars (`width = 2f`, `sizeDelta.y = +6f` extending `3px` above and below the strip, color `rgba(255, 255, 255, 0.38)` with a dark contrast shadow) anchored at `anchorMin.x = anchorMax.x = marks[i]` (`0.155401f`, `0.344599f`, `0.655401f`, `0.844599f`).
   - Placed the `Marker` needle as the last sibling so it slides smoothly across the strip and over the divider lines without any lag.

