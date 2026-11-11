# HTML level extraction and diagnostics specification

Status: VERIFIED FROM SOURCE; runtime tests are separate. Read AGENTS.md, architecture contract v2.8 and contrast-html-parity skill. No gameplay source edited by HTML Analyst.

Reference: `CONTRAST_3_LEVELS_Newton_Fixed_Windowed.html` at project root. Line 25 embeds `window.__CE_LEVELS__`, the authoritative three-level campaign. Existing `Reports/HTML_L01_L02_L03_DEEP_ANALYSIS.md` provides the full object inventory; reuse its tables against line 25, with corrections below. `Reports/HTML_LEVEL_ANALYSIS.md` and older test reports contain stale claims and are not current runtime evidence.

| Level | Name | Authored world w,h | Terrain | Checkpoints incl. spawn | Collectibles | Spawn x,feet,phase |
|---|---|---|---:|---:|---:|---|
| L01 | Cánh cửa không phải vật cản | 6169,1693 | 16 | 6 | 2 | -571,972,pi/2 |
| L02 | Cầu thang ma | 6144,1562 | 15 | 5 | 2 | 110,940,0 |
| L03 | Giếng ba tầng | 6235,3238 | 27 | 9 | 3 | 245,262,pi/2 |

Terrain uses top-left x,y and positive w,h in pixels, y down. Player is 38x64 px (CFG, lines 91–103), spawn x is left edge and feet is bottom. startLevel (1426–1452) sets y=feet-player.h. Thus L01 top-left spawn is (-571,908), center (-552,940).

For existing 64 px/unit conversion, rectangles map to Unity center `((x+w/2)/64,-(y+h/2)/64)`, size `(w/64,h/64)`; player spawn center maps to `((cp.x+19)/64,-(cp.feet-32)/64)`. L01: (-8.625,-14.6875), size (0.59375,1). The initial floor (-662,972,469,69) maps center (-6.6796875,-15.7265625), size (7.328125,1.078125). Conversion is fixed from geometry, never fitted to trace.

## Corrections to prior reports

- Authored world dimensions are bookkeeping data, not collision/camera walls. setupEditorEasyLevel (331–411) derives WORLD sizes from geometry/Newton and sets __CE_FREE_WORLD=true. editorCameraLimit (1764) returns unconstrained values in this mode.
- Authored deathY is null for all three. Actual death uses editorShouldDieFromVoid (1723–1763), called at 1631: descending, at least 560px below last supported/passed floor, and no plausible reachable landing below. It is not a fixed death boundary at 1080 or authored world height.
- A/D move; ArrowLeft/ArrowRight tune phase continuously at 2.75 rad/s (1547–1551). Space/W jump, S accelerates descent (1602–1619). Q/E in rendered hints/prior reports are not actual phase controls here.
- intensityForPhase (538) is `(1+cos(phase))/2`; phase pi/2 is TUNING/GRAY and phase 0 is WHITE. The cosine formula in scratch/generate_level_json.py is correct; older reports describing pi/2 as WHITE are incorrect. Match thresholds: WHITE >=.78, BLACK <=.22, GRAY between (541–551). FIXED is always solid; matching material passes; existing ghost overlap safety remains.
- Terrain counts and ids must come from line 25, not descriptions. No moving authored terrain in L01/L02, but both have two moving Newton rails; therefore moving-object logging remains mandatory.
- Older Reports/04_GAMEPLAY_TEST_RESULTS.md claims 341 PASS. This review did not execute that suite and does not adopt that claim as fresh evidence.

## Newton and motion

All campaign levels execute configured EASY mode. Newton centers/i/k: L01 (5135,659)/3/6; L02 (5110,528)/2/4; L03 (5201,2100)/2/7. Ring scale 220; radii derive from `220*sqrt(n*SOURCE_LAMBDA/MEASURE.referenceLambda)` (1095). Do not replace radii with arbitrary constants.

Rails K/I width,period,angleDeg,direction: L01 (136,10.8,24.4,-1)/(126,8.6,135,+1); L02 (150,11.2,192,-1)/(140,10.5,155,+1); L03 (150,11.2,180,-1)/(140,10.5,155,+1). L01/L03 orbitStart auto; L02 dock. Orbit angle is initial+direction*orbitClock/period*2pi; rectangle center is Newton center+radius*(cos,sin) (847–865). In configured AUTO mode rails rotate from play start; DOCK waits for first grounding on K (1485–1505).

L03 moving terrain: item_mvnodpwx_a offset (0,-127), period4; item_mvnoqbwi_g offset (213,-258), period5; item_mvnp9r6r_o offset (461,676), period5. All phase0/smooth. Travel interpolation is `(1-cos(2pi*((elapsed/period+phase)%1)))/2` (872–887). Unity behavior changes needed for parity require owner approval.

X: keydown adds pressed only when not already held (586–591); keyup deletes held (626–631); update consumes X only after Newton activation and step<3 (1590–1600); pressed clears each fixed step. Before activation X cannot capture. Capturing K requires standing on K and chooses current rail angle freely, fixing K-O-K'; I requires I rail and distance <=48px to axis target; K' requires K rail and <=54px (1192–1244). Wrong rail/misalignment misses. Third capture begins reveal, completion follows reveal duration. Holding/repeat must not duplicate shots; release then repress creates next edge. Space/W remain jump only.

## Safe diagnostic integration recommendation

Append optional observer code inside the existing IIFE just before final `})();`, AFTER collectible wrappers (2920–2937). Wrap the final update binding: snapshot input/position/state before calling original(dt) exactly once, then sample actual final state and infer transitions afterward. Do not replace physics, call update twice, clear keys, alter player/terrain, or add a second simulation loop. Capture keys and pressed before original because pressed is cleared inside update. Preserve original consoleEvent while mirroring existing events into JSONL. Enable with a query flag or window setting; expose explicit export/download API and bounded storage. Snapshot INITIAL_STATE after installing observer because autostart at 2861 has already occurred.

Log simulation step counter, elapsed/dt, runId, actual quangLevelId, player raw top-left/size/velocity/phase/intensity/grounded/groundedOn, before/after/applied delta; terrain objects by stable name and moving rails by name+role; checkpoints, quangPoints and Newton stage/markers. Snapshot state before/after to infer checkpoint/collectible/reveal/death/respawn/completion events, while consoleEvent captures actual shot/lock/miss/input events. Add read-only overlap observations clearly distinguished from collision resolution.

Existing observer APIs: __CE_EDITOR_STATE__ (2860) and __CE_QUANG_STATE__ (3030). They omit full player size/velocity and rail raw rectangles, so they are useful for browser checks but insufficient alone for full logs. Never use T free-move, __CE_TEST_START__ teleport, or __CE_QA_TEST_HOOK__.finish to satisfy gameplay acceptance.

## Executable browser/test options

Verified installed executable paths: `C:/Program Files/Google/Chrome/Application/chrome.exe`, `C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe`, `Y:/Node/node.exe`, Python313, and dotnet. Existing Tests/ContrastTests and Unity EditorLevelTests are documented; no existing browser replay script was found under scratch (only convert_levels.py and generate_level_json.py). Browser CDP or installed browser automation can drive real keydown/up; check package availability before installing anything. Serve localhost or open file URL; retain actual logs. For equivalent scenarios drive timestamped logical inputs through normal browser key events and Unity input pathway. HTML fixed simulation STEP=1/120 (2852), accumulated RAF with max 8 substeps and 50ms clamped RAF delta; document wall-time vs simulated-time distinctions.

