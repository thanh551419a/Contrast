# Preliminary Core inventory from previous Codex static audit — NOT OWNER APPROVED

This is a reminder, not an accepted Core classification. Main Agent and Guardian must verify each item from current source with file:line proof and request the project owner's classification approval before changing methods.

Candidates reported in the previous Codex transcript:
- Core/GameManager.cs: Update, level-loading/restart, transitions.
- Player/PlayerController.cs: ProcessUpdate, ApplyMove, state/color/respawn.
- Player/Input/PlayerInput.cs and keyboard/touch adapters.
- Player/Movement/PlayerMovementResolver.cs, PlayerCollisionResolver.cs, PlayerOverlapResolver.cs, PlayerJumpMotion.cs, MovementGeometry.cs.
- Level/TrajectorySystem.cs, Trajectory.cs, ColorPlatform.cs, LevelMarker.cs, LevelLoader.cs.
- Level/EasyNewtonFinaleController.cs.
- Data/LevelData.cs, LevelDataSanitizer.cs, LevelRepository.cs, LevelSource.cs.
- Color/ColorClassifier.cs, Camera/CameraFollow.cs, gameplay state enums and input bindings in VirtualControlsUI.cs.

Static findings reported, not runtime-verified:
- No demonstrated global deferred Data Event queue and final global commit.
- Newton independent Update has gameplay mutations.
- Level load validation/failure paths differ from Validate-Before-Destroy and Diagnostic Halt requirements.
- Some HTML/JSON/Unity parity gaps for checkpoints, collectibles, Newton constants, and L03 easing.
- Runtime and Level Editor share registries/methods; Editor is a distinct Protected Subsystem.

Do not treat any of these as approved fixes or permission to change platform behavior. Source inspection and runtime testing remain necessary.
