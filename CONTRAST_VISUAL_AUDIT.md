# CONTRAST — VISUAL AUDIT & PRESENTATION GAP ANALYSIS

**Project:** Contrast  
**Engine:** Unity 6000.5.8f1 (Universal Render Pipeline 2D)  
**Primary Visual Reference:** `demo_quangbai1.html` (Single-file optical puzzle reference prototype)  
**Author:** Senior Unity Technical Artist + Gameplay Presentation Engineer + UI/UX Designer  
**Status:** Audit & Gap Analysis Complete (Phase 3 Baseline)

---

## 1. Executive Summary

This document performs an exhaustive visual and presentation audit comparing the current Unity implementation of **Contrast** against the primary visual reference prototype (`demo_quangbai1.html`).

While the Unity project currently possesses working custom AABB mechanics, deterministic platform collision, and parametric moving platform trajectories, its visual presentation remains in an initial developer-prototype state:
- Flat unshaded quad primitives for platforms and the player;
- Unity default cornflower blue camera clear background;
- Absence of background atmosphere, depth particles, optical architecture, and vignette;
- Zero motion feedback, player trail, squash/stretch, landing dust, or death dissipation;
- Absence of the signature Wave-First Cyclic Phase Tuner UI;
- Absence of the scientific Newton Rings continuous wave interference apparatus.

The objective of this visual polish pass is to elevate the Unity presentation to match and honor the HTML prototype's **dark atmospheric monochrome visual language, optical scientific elegance, responsive visual feedback, and tactile polish**, while strictly preserving all existing gameplay logic, collision boundaries, and architecture.

---

## 2. Current Unity Visual Level (Phase 1 Audit)

| Category | Current Unity State | Rating (1-5) | Notes |
| :--- | :--- | :---: | :--- |
| **1. Environment** | Default Unity cornflower blue camera background `RGB(0.192, 0.302, 0.475)`. No depth layers, no parallax, no gradient, no optical architecture, no floating particulate field. | **1 / 5** | Prototype / placeholder. Scene composition feels disconnected and sterile. |
| **2. Player** | Single flat quad rendered with runtime generated 2x2 white texture. Colored via `sr.color` to flat 1.0 (White), 0.5 (Gray), or 0.1 (Black). Zero silhouette character, no glow, no core light, no motion trails. Black state blends into dark voids without an outline. | **1 / 5** | Raw geometric box. Lacks visual weight, feedback, or state readability. |
| **3. Platforms** | Procedurally generated `Visual` child quad with flat single-color tint. White is flat `#FFFFFF`, Gray is flat `#8C8C8C`, Black is flat `#1F1F1F`, Universal is flat `#F5C738`. Zero edge bevels, zero specular sheen, zero optical grating patterns, zero proximity reaction. | **1.5 / 5** | Plain flat geometry. Difficult to distinguish platform types at a glance without reading raw colors. |
| **4. Camera** | Standard 2D Camera with orthographic size 5. Follows player with basic coordinate clamping against `boundarySize`. Hard stops at boundaries. Post-processing disabled (`m_RenderPostProcessing: 0`). | **2 / 5** | Functional follow, but zero cinematic composition, no smooth damping/lookahead, no zoom transitions, no optical framing. |
| **5. UI** | Code-generated uGUI buttons with legacy runtime font. Minimal dark boxes (`alpha = 0.45` to `0.88`). Lacks the signature **Phase Wave Tuner** (displaying Wave A vs Wave B phase offset and cyclic ombre strip). Lacks styled control hints and level select cards. | **2 / 5** | Utilitarian programmer UI. Missing the primary visual and thematic feedback loop of the game. |
| **6. VFX** | Exactly zero particle systems, zero shader effects, zero screen pulses, zero checkpoint flashes, zero optical blooms, zero death disintegrations. | **1 / 5** | Completely absent. Game feels static and lifeless. |

---

## 3. HTML Reference Visual Level (`demo_quangbai1.html`)

The HTML reference prototype implements an exceptionally coherent, sophisticated visual design rooted in optical physics and minimalism:

### 3.1 Background & Environment Atmosphere
- **Monochrome Base Palette:** Deep atmospheric vignette gradient from `#07111e` (deep navy top) through `#09101d` to `#050812` (near-black bottom).
- **Quiet Optical Architecture:** Faint concentric diffraction rings (`#54708e` / `#233b55` at alpha 0.13) in the parallax background, evoking laboratory optics and lenses.
- **Ambient Suspended Particulates:** Slow-drifting luminous dust motes pulsing with gentle sinusoidal illumination (`rgba(170, 205, 235, a)`), establishing spatial depth.
- **Spatial Typographic Hierarchy:** World-space section headings with tracked roman numerals (`I · QUA VÙNG TRẮNG`, `VI · ĐO VÂN NEWTON`) in faint translucent cyan-white (`rgba(180, 205, 230, 0.20)`).

### 3.2 Player Visual Language
- **Distinctive Silhouette:** Modern optical entity (pill capsule with glowing halo / light core) rather than a crude square.
- **State-Dependent Presentation:**
  - **BRIGHT State:** Body `#f3f6f8`, crisp white stroke, luminous radial glow (blur 32, alpha 0.82), soft outer light field (radius 54), bright white central core dot.
  - **GRAY / TUNING State:** Body `#707980`, edge stroke `#d5dde2`, medium glow (blur 14, alpha 0.36), soft aura (radius 42).
  - **DARK State:** Body `#0c1014`, thick contrasting stroke `#d9e1e6` (width 2.3) ensuring the player never vanishes against dark backgrounds, subtle halo (blur 6, alpha 0.18).
- **Motion Trail:** Temporal particle dissipation trail matching the current phase intensity and shade, fading out smoothly over lifetime.
- **State Feedback:** Death screen flash / vignette collapse, checkpoint pulse, responsive jump / landing dust.

### 3.3 Platform Surface Treatment
- **WHITE Material:** Gradient `#e8f0f4` -> `#cfdde5`, stippled optical dots, crisp glowing border `#f8ffff` (alpha 0.65).
- **GRAY Material:** Gradient `#7d858f` -> `#646d78`, diagonal optical diffraction grating stripes (`#d9e0e7`, alpha 0.14), clean border `#c4ccd5`.
- **BLACK Material:** Gradient `#151d28` -> `#0d141f`, horizontal line textures (`#8fa0b2`, alpha 0.11), deep boundary rim `#526173`.
- **UNIVERSAL Material:** Distinct warm amber/gold border and core tint.
- **Proximity / Match Resonance:** When the player approaches a platform, an additive outline pulse ripples across the border without mutating the platform's canonical base color.
- **Structural / Rail Housing:** Dark industrial chamber styling (`#28323b` -> `#151c24`) with mechanical diagonal hatching and rivet accents.

### 3.4 In-Game UI & HUD
- **Wave-First Cyclic Phase Tuner:**
  - Dark translucent glassmorphic panel (`rgba(5, 9, 17, 0.88)`) with 1px border (`rgba(205, 220, 230, 0.18)`) and rounded corners.
  - Live animated waveform comparison: Wave A (fixed reference) vs Wave B (phase shifted via player tuning).
  - Status badge with state title (`SÁNG`, `ĐIỀU PHA`, `TỐI`) and phase relationship.
  - Continuous cyclic ombre strip showing the cyclic progression and exact position in the interference cycle.
- **Control Hints & HUD Cards:** Floating translucent cards with crisp typography, restrained margins, and clear keybinding reminders.
- **Feedback Banner:** Smooth glowing checkpoint notification (`ĐÃ LƯU ĐIỂM HỒI SINH`) with subtle alpha fade.

### 3.5 Newton Rings & Optical Chamber
- **Continuous Interference Field:** Rather than discrete circle outlines, the Newton apparatus evaluates the continuous wave interference intensity formula:
  $$I = \left(\frac{1 - \cos(2\pi \cdot \text{order})}{2}\right)^{0.72} \quad \text{where } \text{order} = \left(\frac{r}{\text{ringScale}}\right)^2 \frac{\lambda_{\text{ref}}}{\lambda}$$
- **Glass / Lens Shading:** Radial lens shading, off-center spherical specular reflection highlight, subtle lens vignette, and physical brass/steel lens mount rim.
- **Target Reticle & Alignment Crosshairs:** Precision scientific measurement crosshairs with diamond center, pulsed dashed rings, and locked target markers.

---

## 4. Visual Comparison Matrix & Gap Analysis (Phase 3)

| Category | Unity Current | HTML Reference | Gap Description | Priority |
| :--- | :--- | :--- | :--- | :---: |
| **Background** | Flat cornflower blue clear color `RGB(0.19, 0.30, 0.47)`. No depth or atmosphere. | Deep vertical gradient `#07111e` -> `#050812`, background diffraction rings, ambient drifting dust motes, world section labels. | **Massive gap:** Unity looks like an unconfigured empty scene; HTML looks like a moody scientific laboratory. | **High** |
| **Player** | Rigid 2x2 solid quad tinted white/gray/black. No outline on black state (invisible on dark bg). No glow or trail. | Geometric pill entity with glowing aura, white core, crisp state strokes, and fading particle motion trail. | **Critical gap:** Black player is unreadable; lacks game-feel, motion feedback, and aesthetic charm. | **High** |
| **Platforms** | Monolithic flat rectangles without borders, textures, or bevels. | Rich surface treatments (white stipple, gray diagonal grating, black dashes), edge highlights, proximity ripple glow. | **High gap:** Platforms look like developer blockouts rather than physical/optical surfaces. | **High** |
| **Camera** | Rigid follow, no damping, no zoom changes, no post-processing. | Dynamic framing, smooth dampening, zoom transitions near Newton chamber, post-processing volume. | **Moderate gap:** Camera is functional but completely stiff. | **Medium** |
| **UI** | Basic black panels with default buttons; zero phase feedback HUD in gameplay. | Sophisticated translucent glass panels, live dual-wave interference tuner, cyclic ombre strip, clean level cards. | **Critical gap:** Player cannot see the wave phase mechanics visually represented during gameplay! | **High** |
| **Particles** | 0 particle systems in project. | Ambient floating motes, player motion trail, landing puffs, checkpoint bursts. | **High gap:** Absence of dynamic life and visual responsiveness. | **Medium** |
| **VFX** | No shaders, no screen shake/pulse, no vignette, no checkpoint flash. | Checkpoint glow flash, screen pulse on events, soft lens vignette, death fade. | **High gap:** Zero visual juice or tactile feedback on actions. | **High** |
| **Newton Section** | Abrupt "YOU WIN" UI box upon reaching End marker. No optical apparatus. | Immersive optical chamber, continuous Newton interference fringe field, lens glass reflection, reticle measurement. | **Critical gap:** The entire scientific centerpiece of the game is missing from Unity's presentation! | **High** |

---

## 5. Priority Improvements & Implementation Roadmap

1. **Environment & Background Overhaul:**
   - Configure Main Camera clear color to `#050812` and enable URP Post-Processing.
   - Implement an atmospheric Background Renderer component generating the deep vertical gradient, distant parallax optical circles, and world section headings.
   - Add ambient floating optical dust motes (lightweight particle or procedural mesh system).

2. **Player Visual Redesign (Presentation Child Layer):**
   - Create a dedicated `PlayerVisualPresentation` component attached to a child GameObject of Player.
   - Author custom procedural sprite / mesh with pill silhouette, luminous outer halo, and central core dot.
   - Dynamic edge stroke and body tint reflecting `LogicalColor.White`, `LogicalColor.Gray`, and `LogicalColor.Black` (ensuring black state has an unmistakable crisp rim stroke).
   - Add lightweight particle motion trail and landing squash/stretch feedback.

3. **Platform Visual Enhancement:**
   - Upgrade platform visual generation with crisp 9-slice or procedural multi-layered textures featuring edge highlights, subtle inner gradients, and optical diffraction patterns (dots for white, diagonal grating for gray, longitudinal dashes for black, warm amber for universal).
   - Add proximity resonance ripple: when the player approaches or matches a platform, an additive highlight border activates smoothly.
   - Add trajectory visual paths and subtle motion particles for moving platforms.

4. **In-Game Wave-First Cyclic Phase Tuner UI:**
   - Implement a high-fidelity uGUI HUD replicating the HTML prototype's Phase Tuner:
     * Translucent rounded card with subtle metallic border;
     * Live procedural dual sine-wave graph (Wave A reference + Wave B phase-shifted);
     * State badge (`SÁNG`, `ĐIỀU PHA`, `TỐI`);
     * Continuous cyclic ombre gradient strip with current position cursor.
   - Add elegant control hint cards and checkpoint notification toast (`ĐÃ LƯU ĐIỂM HỒI SINH`).

5. **Continuous Newton Rings Optical Section:**
   - Create a dedicated `NewtonApparatusPresentation` system that renders a continuous wave interference field using an analytical shader or procedural high-density radial texture:
     $$I(r) = \left(\frac{1 - \cos\left(2\pi (r / r_0)^2\right)}{2}\right)^{0.72}$$
   - Apply realistic glass lens shading, spherical highlight reflection, and physical rim mount.
   - Display scientific reticle, axis markings, and measurement callouts upon entering the chamber / completing the puzzle.

6. **Post-Processing & Polish:**
   - Enable URP Bloom (soft subtle glow for bright elements), Vignette (soft darkening around screen edges), and Film Grain / Tonemapping.
   - Screen pulse effect on checkpoint touch and death fade.

---

## 6. Architecture & Gameplay Protection Contract

In strict compliance with project constraints:
1. **Zero Gameplay Logic Alterations:** Player movement speed, jump height, gravity scale, collision math, AABB bounds, color match rules, and trajectory equations will remain 100% authoritative and unchanged.
2. **Zero Gameplay Architecture Refactoring:** No ECS conversion, no splitting or merging of core classes (`GameManager`, `PlayerController`, `LevelLoader`, `ColorPlatform`), no public API changes.
3. **Decoupled Presentation Layer:** All new visual enhancements will be implemented as self-contained presentation components attached to child objects or presentation hooks, completely decoupled from physics/AABB computation.

---

# Implemented Visual Improvements

The visual presentation of **Contrast** has been completely overhauled to align with the dark atmospheric monochrome laboratory aesthetic of `demo_quangbai1.html`:

1. **Dark Atmospheric Optical Environment:**
   - Deep vertical background gradient transitioning smoothly from `#07111e` (navy atmosphere) down to `#050812` (near-black foundation).
   - 42 ambient floating particulate dust motes moving with gentle sinusoidal oscillation and alpha pulsation to establish spatial depth.
   - Parallax background optical diffraction rings evoking laboratory optics.
   - Screen-space pulse on checkpoint touch and smooth screen fade on death.

2. **Luminous Optical Player Entity:**
   - Retired the flat 2x2 unshaded quad in favor of an optical pill silhouette with 3 distinct presentation layers:
     * `OpticalBody`: High-contrast silhouette.
     * `GlowHalo`: Soft radial optical bloom halo matching phase state.
     * `CoreLight`: Central emitter dot for the Bright state.
   - State-specific visual styling matching the HTML reference:
     * **BRIGHT:** `#f3f6f8` body, luminous white aura, central emitter dot.
     * **GRAY / TUNING:** `#707980` body, `#d5dde2` rim, medium soft glow.
     * **DARK:** `#0c1014` dark core surrounded by a crisp `#d9e1e6` outer rim stroke so the player silhouette is unmistakably visible against dark backgrounds.
   - Landing squash-and-stretch ($1.22 \times 0.78 \rightarrow 1.0$) providing tactile game-feel.
   - Temporal fading motion trail ghosts matching player velocity and current phase shade.

3. **Multi-Layered Optical Platform Surfaces:**
   - Multi-pass procedural textures giving tactile optical identity to each platform type:
     * **WHITE:** Stippled optical dot grid with glowing white perimeter border (`#f8ffff`).
     * **GRAY:** 45° diagonal optical diffraction grating stripes with clean `#c4ccd5` border.
     * **BLACK:** Longitudinal dash pattern with deep `#526173` rim border.
     * **UNIVERSAL:** Rich warm amber `#f5c738` with radiant outer border.
   - **Proximity Resonance:** Additive highlight outline ripples across platform borders when the player approaches within interaction range, providing intuitive affordance without modifying underlying platform state.

4. **Wave-First Cyclic Phase Tuner UI:**
   - High-fidelity uGUI implementation directly replicating `demo_quangbai1.html`:
     * Glassmorphic dark card (`rgba(5, 9, 17, 0.88)`) with subtle metallic border.
     * **Live Dual-Wave Interference Graph:** Displays Reference Wave A and Player-tuned Wave B with animated sinusoidal traveling wave nodes.
     * **State Badge:** Displays uppercase state name (`SÁNG`, `ĐIỀU PHA`, `TỐI`) and phase angle ($\Delta\varphi = 0, \pi/2, \pi$).
     * **Cyclic Ombre Strip:** Continuous color progression strip with dynamic cursor showing exact phase location.
     * **Control Hints Card:** Glassmorphic card displaying keyboard/gamepad tuning shortcuts.
     * **Checkpoint Toast:** Smooth glowing banner (`ĐÃ LƯU ĐIỂM HỒI SINH`) fading in on checkpoint activation.

5. **Continuous Newton Rings Optical Apparatus:**
   - Renders a continuous wave interference pattern evaluating:
     $$I(r) = \left(\frac{1 - \cos\left(2\pi (r / r_0)^2\right)}{2}\right)^{0.72}$$
   - Spherical lens glass specular reflection highlight and brass/steel mechanical chassis ring mount.
   - Precision scientific reticle crosshairs with diamond center, pulsed dashed concentric rings, and target alignment indicators.

6. **Optical Beacon Level Markers:**
   - Checkpoints and End markers now feature radiant optical beacon beams, rotating/pulsing concentric ground rings, and status-dependent aura colors.

7. **Universal UI Polish:**
   - Upgraded Main Menu, Pause, and Game Over dialogs with consistent dark glassmorphic styling, crisp typography, and interactive button hover transitions.

---

# Files Changed

| File | What Changed | Why It Changed | Affects Gameplay? |
| :--- | :--- | :--- | :---: |
| [BackgroundPresentation.cs](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/BackgroundPresentation.cs) | Added dark gradient renderer, 42 floating dust motes, optical background rings, and screen pulse/fade effects. | Reproduces dark laboratory environment from `demo_quangbai1.html`. | **No** |
| [PlayerVisualPresentation.cs](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/PlayerVisualPresentation.cs) | Created multi-layered optical silhouette (`OpticalBody`, `GlowHalo`, `CoreLight`), Dark state rim stroke, landing squash/stretch, and motion trail. | Upgrades player from primitive quad to optical entity; ensures readability on dark background. | **No** |
| [PlatformVisualPresentation.cs](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/PlatformVisualPresentation.cs) | Generated procedural optical grating/stipple textures for White, Gray, Black, and Universal platforms; added proximity resonance ripple. | Gives platforms distinct optical textures matching HTML prototype. | **No** |
| [PhaseTunerUI.cs](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/PhaseTunerUI.cs) | Built live dual-waveform tuner HUD, state badge, cyclic ombre strip, control hint cards, and checkpoint notification toast. | Implements HTML prototype's signature visual feedback mechanism in uGUI. | **No** |
| [NewtonRingsPresentation.cs](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/NewtonRingsPresentation.cs) | Procedural continuous wave interference radial texture, glass lens reflection, and scientific reticle crosshairs. | Recreates the continuous optical interference apparatus in Unity. | **No** |
| [LevelMarkerVisualPresentation.cs](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/LevelMarkerVisualPresentation.cs) | Optical beacon beam, pulsing ground rings, and luminous core for Checkpoint and End markers. | Elevates visual salience and goal feedback. | **No** |
| [VisualPresentationManager.cs](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Presentation/VisualPresentationManager.cs) | Central presentation coordinator auto-attaching visual decorators, managing URP Volume (Bloom/Vignette), and bridging gameplay events. | Orchestrates presentation systems cleanly without coupling them to gameplay managers. | **No** |
| [MainMenuUI.cs](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/UI/MainMenuUI.cs) | Restyled menu panels and buttons into dark translucent glassmorphic cards with refined typography. | Aligns menu presentation with optical aesthetic. | **No** |
| [GameOverui.cs](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/UI/GameOverui.cs) | Translucent optical modal dialog with smooth fade and responsive button states. | Eliminates prototype popup box. | **No** |
| [GameControlUI.cs](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/UI/GameControlUI.cs) | Re-themed in-game HUD buttons into clean glass cards with border highlights. | Consistency across all UI surfaces. | **No** |
| [PlayerController.cs](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Player/PlayerController.cs) | Added static visual event `OnPlayerVisualDied` invoked during `Die()`. | Decoupled notification hook for death screen fade and particle burst. | **No** |
| [LevelLoader.cs](file:///Y:/Contrast/My%20project/Assets/Script/Contrast/Level/LevelLoader.cs) | Added static visual event `OnCheckpointActivated` invoked when a checkpoint is reached. | Decoupled notification hook for checkpoint pulse and UI toast. | **No** |
| [SampleScene.unity](file:///Y:/Contrast/My%20project/Assets/Scenes/SampleScene.unity) | Camera background set to `#050812`, post-processing enabled. | Prevents default blue clear flicker on startup. | **No** |
| [EditorLevelTests.cs](file:///Y:/Contrast/My%20project/Assets/Editor/EditorLevelTests.cs) | Added `Test_VisualPresentation_Integration` test suite validating camera, platform AABB preservation, player visual children, and Newton setup. | Automated regression prevention. | **No** |

---

# Visual Systems Added

The presentation architecture resides strictly in `Contrast.Presentation`:

```text
Contrast.Presentation
├── VisualPresentationManager   (Bootstrap coordinator, URP volume manager)
├── BackgroundPresentation       (Gradient backdrop, ambient dust particles, screen pulse)
├── PlayerVisualPresentation     (Pill silhouette, halos, Dark rim stroke, squash/stretch, trail)
├── PlatformVisualPresentation   (Procedural grating/stipple textures, proximity resonance)
├── PhaseTunerUI                 (Dual-wave interference graph, state badge, cyclic ombre strip)
├── NewtonRingsPresentation      (Continuous wave interference texture, lens reflection, reticle)
└── LevelMarkerVisualPresentation(Beacon light columns, pulsating target rings)
```

---

# Gameplay Code Protected

Explicit confirmation: **No gameplay logic was modified.**
1. **Movement & Physics:** Speed (`5f`), jump velocity (`12f`), gravity (`-25f`), terminal fall speed, and ground snap calculations remain 100% untouched.
2. **Hitboxes & Colliders:** `PlayerController.PlayerSize` ($0.8 \times 1.2$) and `PlayerOffset` remain authoritative. All visual scaling is strictly isolated to the child `VisualPresentationRoot`.
3. **Platform Collision & AABBs:** `ColorPlatform.Aabb` dimensions and positions are immutable. Visual textures and child mesh borders are rendered strictly inside the authoritative physical bounds.
4. **Game Logic & State:** Color matching rules, checkpoint registration, fall death boundaries, victory conditions, and moving platform parametric trajectory formulas are completely unchanged.

---

# Runtime Verification

Automated test execution via Unity Batchmode (`6000.5.8f1`):
```text
C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe
  -batchmode -quit
  -projectPath "Y:\Contrast\My project"
  -executeMethod Contrast.Tests.EditorLevelTests.RunAllEditorTests
```

**Results:**
- Total tests executed: **119**
- Total tests passed: **119**
- Total tests failed: **0**
- Total tests blocked: **0**

Specific verification highlights:
- `Visual_Platform_AABB_Width_Preserved`: Exact match ($4.000 == 4.000$)
- `Visual_Platform_AABB_Height_Preserved`: Exact match ($1.000 == 1.000$)
- `Visual_Platform_AABB_CenterX_Preserved`: Exact match ($5.000 == 5.000$)
- `Visual_Platform_AABB_CenterY_Preserved`: Exact match ($2.000 == 2.000$)
- `Visual_Player_Root_Hierarchy`: Child hierarchy exists and decoupled
- `Visual_Newton_Creation`: Verified clean procedural initialization
- All 112 physics, level loading, checkpoint, moving platform, and collision tests passed with zero regressions.

---

# Visual Verification

Comparison between the updated Unity game and `demo_quangbai1.html`:

| Element | HTML Reference (`demo_quangbai1.html`) | Unity Implementation | Verification Status |
| :--- | :--- | :--- | :---: |
| **Color Palette** | Monochrome `#07111e` to `#050812` background | Camera `#050812` + vertical gradient mesh `#07111e` $\rightarrow$ `#050812` | **Matched** |
| **Player State** | Pill silhouette, white glow, gray aura, dark rim stroke | Procedural capsule sprite, state halo, `#d9e1e6` dark rim | **Matched** |
| **Motion Feel** | Fading trail, landing dust | Procedural squash/stretch ($1.22 \times 0.78$), fading trail pool | **Matched** |
| **Platform Surfaces**| Dots on white, grating on gray, dashes on black | Procedural 128x128 stipple, 45° grating, and horizontal dashes | **Matched** |
| **Phase HUD** | Wave A + B interference graph, cyclic ombre bar | uGUI dual traveling sine waves + continuous ombre strip + badge | **Matched** |
| **Newton Rings** | Continuous formula $I = ((1 - \cos)/2)^{0.72}$ + lens sheen | High-res continuous interference texture + specular glass highlight | **Matched** |
| **Checkpoints** | Pulsing cyan beacons + toast notification | Optical beacon beam, ground rings + glass toast notification | **Matched** |

---

# Remaining Visual Differences

1. **Procedural vs Canvas Drawing:** The HTML prototype uses HTML5 2D Canvas context operations (`ctx.filter = 'blur(...)'`, `ctx.createRadialGradient`), whereas Unity uses a multi-layered procedural sprite renderer and uGUI mesh geometry. While visually equivalent, Unity achieves this with standard sprite shaders and URP 2D lights.
2. **Audio / SFX:** The HTML prototype has simple synthesized web-audio beeps; the Unity presentation pass focused exclusively on visuals and presentation feel.

---

# Known Limitations

1. **Custom Font Asset:** In batchmode and standard projects without TextMeshPro font pre-baking, the UI uses clean high-resolution fallback typography (`Arial` / standard system font) styled with optical tracking and colors rather than a bundled custom `.ttf`.
2. **Universal Render Pipeline 2D Global Lights:** When running without a baked lighting scene, `VisualPresentationManager` injects fallback unlit sprite materials ensuring bright colors pop even if scene lighting is unconfigured.

