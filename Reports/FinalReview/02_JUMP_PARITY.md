# 02 — PLAYER JUMP DISTANCE & TRAJECTORY PARITY (`Reports/FinalReview/02_JUMP_PARITY.md`)

## 1. HTML Jump & Movement Equations (`CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html`)

In `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html` (`updatePlayer(dt)`), with world coordinate scale `1 Unity unit = 64 HTML pixels`:

- **Horizontal speed (`moveSpeed`):**
  $$v_x = 360\text{ px/s} = \frac{360}{64} = 5.625\text{ units/s}$$
- **Jump impulse (`jumpVel`):**
  $$v_{y0} = 820\text{ px/s} = \frac{820}{64} = 12.8125\text{ units/s}$$
- **Downward gravity (`gravity`):**
  $$g = 1650\text{ px/s}^2 = \frac{1650}{64} = 25.78125\text{ units/s}^2$$
- **Terminal fall speed (`maxFallSpeed`):**
  $$v_{y,\max} = 1050\text{ px/s} = \frac{1050}{64} = 16.40625\text{ units/s}$$
- **Derived Jump Trajectory Metrics:**
  - **Time to Apex ($t_{\text{apex}}$):**
    $$t_{\text{apex}} = \frac{v_{y0}}{g} = \frac{12.8125}{25.78125} \approx 0.49697\text{ s}$$
  - **Max Jump Apex Height ($H_{\max}$):**
    $$H_{\max} = \frac{v_{y0}^2}{2g} = \frac{12.8125^2}{2 \times 25.78125} \approx 3.183712\text{ units}\ (203.76\text{ px})$$
  - **Total Flat-Ground Air Time ($T_{\text{air}}$):**
    $$T_{\text{air}} = 2\,t_{\text{apex}} \approx 0.99394\text{ s}$$
  - **Maximum Flat Horizontal Jump Range ($\Delta X_{\max}$):**
    $$\Delta X_{\max} = v_x \cdot T_{\text{air}} = 5.625 \times 0.99394 \approx 5.5909\text{ units}\ (357.82\text{ px})$$

---

## 2. Cause of Overshoot in Unity Before Fix

- `PlayerController.moveSpeed` was `7.0f` units/s (`448 px/s`, $+24.4\%$ faster than HTML).
- `PlayerJumpMotion.jumpHeight` was `3.6f` units (`230.4 px`, $+13.1\%$ higher than HTML).
- `Rigidbody2D.gravityScale` in `SampleScene.unity` was `3.0f` ($g = 29.43\text{ units/s}^2$), yielding an air time of $0.989\text{ s}$ and a horizontal jump range of $7.0 \times 0.989 = 6.924\text{ units}$ ($443.1\text{ px}$), which was **$+23.8\%$ longer** than HTML (`5.5909 units`).

---

## 3. Parameter Comparison Table (`Before` vs `After` vs `HTML`)

| Parameter | HTML Demo (`px` / `units`) | Unity Before | Unity After | Parity Error |
| :--- | :--- | :--- | :--- | :--- |
| **Horizontal Speed ($v_x$)** | `360 px/s` (`5.625 units/s`) | `7.000 units/s` | `5.625 units/s` | `0.00%` |
| **Gravity ($g$)** | `1650 px/s²` (`25.78125 units/s²`) | `29.430 units/s²` (`scale 3.0`) | `25.78125 units/s²` (`scale 2.628058`) | `0.00%` |
| **Initial Jump Velocity ($v_{y0}$)** | `820 px/s` (`12.8125 units/s`) | `14.557 units/s` | `12.8125 units/s` | `0.00%` |
| **Apex Jump Height ($H_{\max}$)** | `203.76 px` (`3.183712 units`) | `3.600 units` | `3.183712 units` | `0.00%` |
| **Max Fall Speed ($v_{y,\max}$)** | `1050 px/s` (`16.40625 units/s`) | `18.000 units/s` | `16.40625 units/s` | `0.00%` |
| **Total Air Time ($T_{\text{air}}$)** | `0.9939 s` | `0.9893 s` | `0.9939 s` | `0.00%` |
| **Max Horizontal Jump Distance ($\Delta X_{\max}$)** | `357.82 px` (`5.5909 units`) | `6.924 units` | `5.5909 units` | `0.00%` |

---

## 4. Modified Files

- `Assets/Script/Contrast/Player/PlayerController.cs` (`moveSpeed = 5.625f`)
- `Assets/Script/Contrast/Player/Movement/PlayerJumpMotion.cs` (`jumpHeight = 3.183712f`, `maxFallSpeed = 16.40625f`, `HtmlGravityMagnitude = 25.78125f`)
- `Assets/Scenes/SampleScene.unity` (`moveSpeed: 5.625`, `jumpHeight: 3.183712`, `maxFallSpeed: 16.40625`, `m_GravityScale: 2.628058`)

