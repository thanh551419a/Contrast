# CONTRAST — PLAYTEST INSTRUCTIONS & USER MANUAL

Welcome to the playtest build of **CONTRAST**, corrected and updated to match the authoritative gameplay of `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html`.

---

## 1. How to Launch the Game

1. Navigate to the release folder:
   ```text
   Release/Windows/Game/
   ```
2. Double-click **`Game.exe`**.
3. On startup, the game will present the Main Menu and Level Selection screen.
4. Select one of the three official campaign levels:
   - **L01: Cánh cửa không phải vật cản** (Door is not an obstacle)
   - **L02: Cầu thang ma** (Ghost staircase)
   - **L03: Giếng ba tầng** (Three-tier well)

*(Optional: You may also run the standalone Level Editor independently via `Release/Windows/LevelEditor/LevelEditor.exe`)*.

---

## 2. Keyboard Controls (Windows)

All controls strictly adhere to the official HTML reference:

| Key | Action | In-Game Behavior |
|---|---|---|
| **A** | Move Left | Moves player left. |
| **D** | Move Right | Moves player right. |
| **W** or **Space** | Jump | Normal jump. **Space is strictly for jumping** and never triggers Newton marks. |
| **S** | Fast Fall | Accelerates downward descent while in the air. |
| **Left Arrow (←)** | Tune Phase Left | Continuously shifts player phase towards Black ($I \le 0.22$). Does **not** move horizontally. |
| **Right Arrow (→)** | Tune Phase Right | Continuously shifts player phase towards White ($I \ge 0.78$). Does **not** move horizontally. |
| **X** | **Newton Mark** | **Exclusive measurement input.** Used inside Newton Rings chamber to lock $K$, $I$, and $K'$. |
| **R** | Quick Restart | Reloads the current level / respawns at the latest checkpoint. |
| **Escape** | Pause / Menu | Toggles pause menu. |

*(Note: On Android or touch screen devices, virtual touch buttons `A / D`, `W / JUMP`, `▼ FALL`, `◄ PHASE`, `PHASE ►`, and `X (MARK)` provide 1:1 identical functionality).*

---

## 3. Phase & Color Mechanics

Player color is determined continuously by the optical intensity formula:
$$I = \frac{1 + \cos(\text{phase})}{2}$$

- **WHITE ($I \ge 0.78$):** You can pass through White platforms; you collide with Black and Gray platforms.
- **GRAY ($0.22 < I < 0.78$):** You can pass through Gray platforms; you collide with White and Black platforms.
- **BLACK ($I \le 0.22$):** You can pass through Black platforms; you collide with White and Gray platforms.
- **FIXED (Striped / Neutral):** Solid terrain regardless of player phase.

Use **Left Arrow** and **Right Arrow** to adjust your phase to pass through barriers or land on solid platforms!

---

## 4. How to Complete the Newton Rings Measurement Stage

At the end of each level, you will enter the **Newton Rings Apparatus Chamber**. The camera will frame the optical center $O$ and the two concentric orbiting platforms ($K$ and $I$).

### The 3-Step Marking Sequence (Press `X`):

1. **Step 1 — Mark Point $K$ (Rail $k$, White Platform):**
   - Jump onto the outer orbiting platform ($K$).
   - Press **`X`**.
   - Your current position is locked as point $K$. The reference diameter line $K–O–K'$ is established through the optical center $O$.
   - **HUD updates to:** `"TÌM I TRÊN VÂN i · Đứng trên thanh I, căn đúng trục K–O và nhấn X"`.

2. **Step 2 — Mark Point $I$ (Rail $i$, Gray Platform):**
   - Jump onto the inner orbiting platform ($I$).
   - Watch the platform orbit towards the dashed reference axis line $K–O$.
   - When the center of platform $I$ aligns with the axis line (tolerance $\le 48\text{ px}$ / $0.75\text{ units}$), press **`X`**.
   - If correctly aligned, point $I$ is locked with your alignment accuracy percentage (e.g. `100%`).
   - If misaligned, the HUD prompts: `"CHỜ I THẲNG HÀNG VỚI TRỤC K–O"`. (You can retry as many times as needed).
   - **HUD updates to:** `"TÌM K′ TRÊN VÂN k · Quay lại thanh K, căn phía đối diện và nhấn X"`.

3. **Step 3 — Mark Point $K'$ (Rail $k$, Opposite Side of $O$):**
   - Jump back onto outer platform $K$.
   - Ride or wait until platform $K$ reaches the opposite side of $O$ along the reference line.
   - When aligned (tolerance $\le 54\text{ px}$ / $0.84375\text{ units}$), press **`X`**.
   - Point $K'$ is locked!

4. **Step 4 — Measurement Reveal & Victory:**
   - The HUD displays: `"ĐÃ HOÀN THÀNH K – I – K′"`.
   - The physical calculation card appears, showing:
     $$B = KI = r_k + r_i \quad \text{and} \quad b = IK' = r_k - r_i$$
     $$\lambda = \frac{B \cdot b}{R(k - i)} = 550.0\text{ nm}$$
   - After a 5.6-second reveal sequence, the level completion screen activates to advance to the next level.

---

## 5. Level Overview & Specifics

### Level 01 — Cánh cửa không phải vật cản
- **Platform Blocks:** 16 terrain blocks (6 White, 4 Gray, 5 Black, 1 Fixed).
- **Checkpoints:** 6 checkpoints.
- **Quang Điểm (Collectibles):** 2 (optional collection).
- **Newton Orbit Parameters:** $i = 3$, $k = 6$, `orbitStart = auto`.

### Level 02 — Cầu thang ma
- **Platform Blocks:** 15 terrain blocks (3 White, 2 Gray, 8 Black, 2 Fixed).
- **Ghost Block:** `L02_C_GHOST_WHITE` is material **BLACK** (matching HTML source).
- **Checkpoints:** 5 checkpoints.
- **Quang Điểm (Collectibles):** 2 (optional collection).
- **Newton Orbit Parameters:** $i = 2$, $k = 4$, `orbitStart = dock` (platforms start orbiting when you first board platform $K$).

### Level 03 — Giếng ba tầng
- **Platform Blocks:** 27 terrain blocks (5 White, 8 Gray, 5 Black, 9 Fixed).
- **Moving Platforms:** 3 dynamic elevators:
  - `item_mvnodpwx_a`: Vertical descent ($dy = -127\text{ px}$, $T = 4.0\text{ s}$).
  - `item_mvnoqbwi_g`: Diagonal traverse ($dx = 213\text{ px}$, $dy = -258\text{ px}$, $T = 5.0\text{ s}$).
  - `item_mvnp9r6r_o`: High-tier well lift ($dx = 461\text{ px}$, $dy = +676\text{ px}$, $T = 5.0\text{ s}$).
- **Checkpoints:** 9 checkpoints.
- **Quang Điểm (Collectibles):** 3 (optional collection).
- **Newton Orbit Parameters:** $i = 2$, $k = 7$, `orbitStart = auto`.

---

## 6. How to Inspect Logs

If you encounter unexpected behavior or wish to verify raw telemetry:

1. **Unity Standalone Player Log:**
   Located at:
   ```text
   %USERPROFILE%\AppData\LocalLow\ContrastTeam\Contrast\Player.log
   ```
2. **Key Log Tags to Look For:**
   - `[INPUT]`: Raw keystroke detection and mapped `PlayerInputIntent`.
   - `[FLOW]`: Level loading, player spawn, and state transitions (`Loading` $\to$ `Playing` $\to$ `LevelComplete`).
   - `[NEWTON_FINALE]`: Rail detection, alignment error calculations, tolerances, and state transitions.

Have fun playtesting **CONTRAST**!
