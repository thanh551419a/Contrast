# Contrast Runtime Level Editor

## Flow
- Main Menu -> PLAY GAME: loads the configured level JSON through LevelLoader.
- Main Menu -> EDIT LEVEL: opens the runtime editor.
- IMPORT: choose a JSON file in Unity Editor. The existing LevelLoader builds the map.
- EXPORT: serializes the current runtime platform and marker state back to JSON.

## Editor interaction
- **Right palette**: drag PLATFORM, START POS, CHECKPOINT or END into the map.
- Only one START POS and one END are allowed by the current JSON schema. Multiple CHECKPOINT entries are supported; export preserves all checkpoints.
- **Platform selection & resizing**:
  - Click a platform to select it. Four white handles appear (left, right, top, bottom). Drag a handle to resize. Drag the platform body to move it.
  - Moving a platform body automatically translates its trajectory origin and spatial bounds in lockstep.
- **Platform deletion**:
  - A red **DELETE PLATFORM** button appears in the left Inspector when a platform is selected.
  - The button is disabled when no platform is selected and never affects level markers.
  - Shortcut: Pressing `Delete` or `Backspace` deletes the selected platform, safely guarded so typing inside any InputField never triggers deletion.
  - Deletion removes the platform from `ColorPlatform.All`, destroys its GameObject, cleans up handles/UI, and excludes it immediately from JSON export.
- **Trajectory editing**:
  - When a platform is selected, a collapsible **TRAJECTORY** panel appears in the left Inspector.
  - Toggle `Enabled` to activate trajectory motion (disabled by default for new platforms and legacy levels).
  - **Quick presets**:
    - `Line H`: Horizontal line, PingPong, length 5.
    - `Line V`: Vertical line, PingPong, length 5.
    - `Circle`: Full 360° circular orbit, Loop.
    - `Arc 180°`: Half-circle 180° arc, PingPong (reverses seamlessly without teleporting).
  - **Curve types**:
    - `Line`: Basis `C(u) = (u, 0)`. Configurable origin, rotation angle (degrees), and scale/length.
    - `Circle`: Basis `C(theta) = (cos(theta), sin(theta))`. Configurable center (origin), radius, and angular domain. Full circle and arc share this evaluator.
  - **Domain modes**:
    - `ParameterRange`: Uses min/max parameters (u coordinate for Line, angles in degrees for Circle).
    - `SpatialBounds` (Line only): Clips the infinite line against an axis-aligned bounding box defined by X Min/Max and Y Min/Max.
  - **Playback modes**:
    - `PingPong`: Forward and backward traversal. Duration represents total time for a full trip out and back.
    - `Loop`: Periodic continuous traversal. Duration represents one full cycle.
    - `Once`: Traverses from start to end and stops.
  - **On-map previews & draggable handles**:
    - Trajectory path is drawn as a colored preview line directly in the scene/game view.
    - Axis-aligned bounds rectangle is drawn when `SpatialBounds` is enabled.
    - Yellow handle: Origin / Center.
    - Cyan handle: Line endpoint (adjusts rotation and length).
    - Magenta handle: Circle radius.
    - Lime handles: Arc start and end angles.
    - Orange handles: Spatial bounds box edges (X/Y Min/Max).
- **Rider-carry & gameplay**:
  - Trajectory movement runs only while in the `Playing` state (never in Editor or Main Menu).
  - When the player stands on top of a moving platform, its frame displacement is passed into the predictive AABB movement resolver, preserving full collision slide along walls and logical-color blocking rules.
- **Camera panning**:
  - Hold the middle mouse button over the map to pan the editor camera. Panning is editor-only and does not modify level object coordinates.
- **UI input guarding**:
  - If the pointer enters an editor UI panel while dragging, the map drag is canceled so objects cannot follow the cursor over UI controls.

## Runtime build
- File import picker uses `UnityEditor.OpenFilePanel` when running inside the Unity Editor.
- Non-editor builds fall back to `LevelEditorController.ImportJson(path)`, and export goes to `Application.persistentDataPath`.
- JSON schema backward-compatible: missing or disabled `trajectory` field loads platforms as static geometry exactly as before.
