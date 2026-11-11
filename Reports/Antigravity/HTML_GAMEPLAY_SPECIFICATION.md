# HTML GAMEPLAY SPECIFICATION: CONTRAST REFERENCE RE-ENGINEERING

**Authoritative Reference File:** `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html`  
**Reference Origin:** Line 25 (`window.__CE_LEVELS__`), DOM Canvas Rendering Context  
**Specification Date:** November 2026  
**Status:** Canonical Reference Standard for Unity Port  

---

## 1. Coordinate System, Canvas & Units

### 1.1 HTML Coordinate Frame
- **Origin $(0,0)$:** Top-Left corner of the canvas / world coordinate space.
- **X-Axis:** $+X$ points strictly to the **Right**.
- **Y-Axis:** $+Y$ points strictly **Downwards**.
- **Platform Representation:** Expressed as bounding rectangles:
  $$\text{Rect} = [x_{\text{left}}, y_{\text{top}}, \text{width}, \text{height}]$$

### 1.2 Unity World Space Mapping
- **Pixels Per Unit (PPU):** $64.0\text{ pixels} = 1.0\text{ Unity world unit}$.
- **X-Axis Mapping:** $+X$ points **Right** (identical direction).
- **Y-Axis Mapping:** $+Y$ points **Upwards** (inverted direction).
- **Mathematical Conversion Equations:**
  $$X_{\text{Unity}} = \frac{x_{\text{HTML}} + \frac{w_{\text{HTML}}}{2}}{64}$$
  $$Y_{\text{Unity}} = -\frac{y_{\text{HTML}} + \frac{h_{\text{HTML}}}{2}}{64}$$
  $$\text{Width}_{\text{Unity}} = \frac{w_{\text{HTML}}}{64}, \quad \text{Height}_{\text{Unity}} = \frac{h_{\text{HTML}}}{64}$$

---

## 2. Player Dimensions & Kinematics

### 2.1 Player Geometry
- **HTML Dimensions:** $38\text{ px width} \times 64\text{ px height}$.
- **Unity Dimensions:** $0.59375\text{ units width} \times 1.0\text{ unit height}$.
- **Origin / Pivot:** Center of bounding box.
- **Feet Coordinate Offset:** $Y_{\text{feet}} = Y_{\text{center}} - \frac{\text{Height}}{2} = Y_{\text{center}} - 0.5\text{ units}$ (Unity).

### 2.2 Horizontal Movement Physics
- **Max Speed ($v_{x,\max}$):** $360\text{ px/s} = 5.625\text{ units/s}$.
- **Acceleration:** Instantaneous responsive acceleration ($a_x \to \infty$) with standard damping on release.
- **Directional Input:**
  - $A$ / $\leftarrow$: Move Left ($\Delta x < 0$)
  - $D$ / $\rightarrow$: Move Right ($\Delta x > 0$)

### 2.3 Vertical Movement & Jump Physics
- **Initial Jump Velocity ($v_{y,0}$):**
  - HTML: $-820\text{ px/s}$ (upwards in HTML $-Y$)
  - Unity: $+12.8125\text{ units/s}$ ($820 / 64$, upwards in Unity $+Y$)
- **Gravity Acceleration ($g$):**
  - HTML: $+1650\text{ px/s}^2$ (downwards)
  - Unity: $-25.78125\text{ units/s}^2$ ($1650 / 64$, downwards)
- **Terminal Fall Velocity ($v_{\text{term}}$):**
  - HTML: $+1200\text{ px/s}$
  - Unity: $-18.75\text{ units/s}$
- **Jump Cutoff / Variable Jump:** Releasing the jump button early dampens upward velocity by factor of $0.5$ if $v_y > 0$.
- **Fast Fall:** Pressing $S$ / $\downarrow$ increases downward gravity by $1.5\times$ or adds an immediate downward impulse.

---

## 3. Phase Tuning & Color Mechanics

### 3.1 Mathematical Intensity Function
The player's internal state maintains a continuous phase angle $\theta$:
$$\theta \in [0, 2\pi), \quad \frac{d\theta}{dt} = 2.75\text{ rad/s}$$

The visual and physical color intensity $I \in [0, 1]$ is computed as:
$$I = \frac{1 + \sin(\theta)}{2}$$

### 3.2 Color Classification Thresholds
| Physical Color Category | Intensity Range ($I$) | Visual RGB Output | Gameplay Interaction |
| :--- | :--- | :--- | :--- |
| **White** | $I \ge 0.78$ | RGB(255, 255, 255) | Passes through White platforms; solid to Black/Universal |
| **Black** | $I \le 0.22$ | RGB(20, 20, 20) | Passes through Black platforms; solid to White/Universal |
| **Gray (Neutral / Transition)** | $0.22 < I < 0.78$ | Interpolated Gray | Solid to both White and Black platforms |
| **Universal Platform** | `Material = 255` | Distinct Neutral Tint | **Always solid** regardless of player intensity |

### 3.3 Pass-Through & Intangibility Rule
- When a player is classified as **White** ($I \ge 0.78$), all White terrain tiles become non-colliding (intangible), allowing the player to fall or walk through them.
- When a player is classified as **Black** ($I \le 0.22$), all Black terrain tiles become non-colliding.
- If a player tunes to Black while standing on a Black platform, they immediately lose ground support and fall through.

---

## 4. Moving Platforms & Trajectory System

Platforms in HTML support scripted trajectories defined by waypoints or oscillation curves:
- **Linear Translation:** Platforms interpolate between points $(X_0, Y_0)$ and $(X_1, Y_1)$ at velocity $V$.
- **Dwell Time:** Upon reaching an endpoint, platforms pause for $T_{\text{dwell}}$ (typically $0.5\text{s}$ to $1.0\text{s}$) before reversing.
- **Rider Coupling:** Any actor whose bottom edge is in contact with the platform top edge ($\Delta y \le \epsilon$) receives the exact displacement $\Delta \vec{P}_{\text{platform}}$ in addition to its own movement.

---

## 5. Checkpoints & Respawn Mechanics

### 5.1 Checkpoint Geometry
- Authoring: Checkpoint regions are defined as bounding rectangles $[x, y, w, h]$.
- Respawn Location: Calculated from the bottom center of the checkpoint trigger:
  $$X_{\text{spawn}} = \frac{x_{\text{HTML}} + 19}{64}$$
  $$Y_{\text{spawn}} = -\frac{y_{\text{HTML},\text{bottom}} - 32}{64}$$

### 5.2 Activation
- Checkpoint 0 (`CP0`) is active by default at level start.
- Touching subsequent checkpoint bounds updates the current respawn index and activates visual feedback.
- On death (falling into void or environmental hazard), player position is reset to the active checkpoint coordinates and velocity is zeroed.

---

## 6. Newton Ring Finale Mechanics

### 6.1 Scientific Principles & Formulas
The finale chamber in Level 1 models physical Newton's rings formed by interference between a spherical lens of radius $R$ and a flat glass plate:
$$r_m = \sqrt{m \cdot R \cdot \lambda}$$
Where:
- $m$: Ring order (integer $1, 2, 3\dots$)
- $R$: Radius of lens curvature ($R = 1.0\text{ m}$)
- $\lambda$: Target wavelength ($\lambda = 550.0\text{ nm} = 5.5 \times 10^{-7}\text{ m}$)

### 6.2 Interactive Workflow
1. **Trigger:** Player enters the Newton measurement zone at the end of Level 1.
2. **Step 1 (Ring K Measurement):** Player adjusts slider $K$ to align reticle with dark ring $m=1$. Pressing `X` captures reading. Tolerance: $|\Delta r| \le 48\text{ px}$ ($0.75\text{ units}$).
3. **Step 2 (Ring I Measurement):** Player adjusts slider $I$ to align reticle with ring $m=2$. Pressing `X` captures reading. Tolerance: $|\Delta r| \le 54\text{ px}$ ($0.84\text{ units}$).
4. **Step 3 (Ring K' Measurement):** Verification measurement across opposite quadrant.
5. **Step 4 (Calculation & Verification):** System evaluates:
   $$\lambda_{\text{calc}} = \frac{r_m^2 - r_n^2}{(m - n) R}$$
   Must equal $550.0\text{ nm} \pm 10\text{ nm}$.
6. **Step 5 (Reveal & Victory):** Lens glows, victory door opens, and level exit unlocks.

---

## 7. Input Control Scheme Summary

| Action | Primary Key | Secondary Key | HTML Handler | Unity Adapter |
| :--- | :--- | :--- | :--- | :--- |
| **Move Left** | `A` | `LeftArrow` | `keydown` code `KeyA` / `ArrowLeft` | `StandaloneKeyboardInputAdapter` |
| **Move Right** | `D` | `RightArrow` | `keydown` code `KeyD` / `ArrowRight` | `StandaloneKeyboardInputAdapter` |
| **Jump** | `Space` | `W` / `UpArrow` | `keydown` code `Space` / `KeyW` | `StandaloneKeyboardInputAdapter` |
| **Fast Fall** | `S` | `DownArrow` | `keydown` code `KeyS` / `ArrowDown` | `StandaloneKeyboardInputAdapter` |
| **Phase Tune (Clockwise)** | `E` | `L` | `keydown` code `KeyE` / `KeyL` | `StandaloneKeyboardInputAdapter` |
| **Phase Tune (Counter-CW)** | `Q` | `J` | `keydown` code `KeyQ` / `KeyJ` | `StandaloneKeyboardInputAdapter` |
| **Newton Mark** | `X` | — | `keydown` code `KeyX` (Edge Triggered) | `StandaloneKeyboardInputAdapter` |
