# Architecture Guardian review — gameplay parity continuation

Date: 2026-11-02. Read-only review of implementation/source; this report does not approve protected changes. Contract v2.8 and provisional inventory read. The Core inventory remains NOT OWNER APPROVED.

## Reviewed scope

- Existing architecture/current findings reused from CONTRAST_ARCHITECTURE_CURRENT_STATE.md and docs/architecture/CORE_INVENTORY_AUDIT_NOT_APPROVED.md, checked against current source where relevant.
- Canonical L01/L02/L03 JSON diff adds collectible position/size and Newton enabled/vestibuleId, retaining raw source fields. It does not change platforms, phase, trajectories, or Editor code.
- Root HTML diagnostic insertion wraps final update once and mirrors consoleEvent; it reads actual state and writes only diagnostic storage. It adds no second simulation loop and does not clear/modify input or player/terrain.
- New GameplayParityObserver.cs reads public state in LateUpdate, observes existing log messages and Player.OnNewtonMarkRequested, and writes an opt-in CreateNew output file. No gameplay-state writes or execution-order override were found in reviewed version.
- Planned NEW opt-in replay harness uses existing normal GameManager.PlayLevelFromFile and PlayerInput.SetCustomAdapter seams. This is test setup/input injection, not a Core edit. It may proceed only if it never teleports, forces grounded/success/capture state, calls an alternate ProcessUpdate/commit loop, or changes normal frame ordering. Final source must still be checked.

Status: NON-CORE SCOPE COMPATIBLE for the new observer/test-support boundaries above. This is a classification/review finding, not owner approval of Core or platform changes.

## Actual phase and risks retained

GameManager.Update calls TrajectorySystem.UpdateAll, rider-displacement setup, PlayerController.ProcessUpdate, LevelLoader.ProcessPlayerInteractions, then camera. PlayerController invokes input and commits through ApplyMove. This does not prove an existing global deferred data-event queue. No new pipeline is authorized.

EasyNewtonFinaleController.Update (782–850) independently handles zone/dock state, reveal animation, and capture. Its IsNewtonMarkPressedThisFrame (852–863) reads player.NewtonMarkRequested, then falls back to physical X. StartRevealSequence (1045+) changes player input/movement and camera through public calls. These are existing protected paths, not newly approved fixes. The requested earlier Update edit remains UNAPPROVED and UNAPPLIED.

LevelLoader.BuildRuntimeLevel creates platforms/checkpoints/end and Newton but does not consume data.collectibles. Adding JSON fields cannot instantiate missing collectibles. Integrating a new collector through the protected loader/interactions span requires a separate exact design/diff and owner approval; adding an independent collector Update would violate AC-018.

## Specific protected proposal — UNAPPLIED, NOT APPROVED

File: Assets/Script/Contrast/Level/EasyNewtonFinaleController.cs
Class/method: EasyNewtonFinaleController.Update, beginning at line 782.
Current behavior: Update is not gated by GameManager.State. The GameManager main pipeline returns outside Playing, while this method can still update dock/reveal/capture when its component/player survive. Source proves missing gate; runtime reproduction is still required before marking the resulting user-visible failure as verified.

Minimal proposal for that one issue only:

```diff
--- a/Assets/Script/Contrast/Level/EasyNewtonFinaleController.cs
+++ b/Assets/Script/Contrast/Level/EasyNewtonFinaleController.cs
@@
         private void Update()
         {
+            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
+                return;
+
             if (player == null)
             {
                 player = FindAnyObjectByType<PlayerController>();
```

Reason: prevent this existing feature loop from processing gameplay while the coordinator is paused/loading/menu/game-over. This does NOT move Newton into the required aggregation/commit phases, remove physical-X fallback, or resolve uncertain execution order. Those require separately designed spans and fresh approval.

Call/dependency impact: Unity invokes Update; it reads GameManager.Instance/State, then existing player/camera/platform/feedback/capture logic. The gate changes when all downstream logic is reachable, including visual feedback. No schema, Editor method, or platform trajectory calculation changes. Shared runtime Test Play still uses the same GameManager state; verify editor Test Play exits and pause/resume behavior. Owner approval remains necessary because this is a provisional Core span and changes existing gameplay reachability.

Tests before acceptance: reproduce X/reveal while paused; verify no progression while paused, resume retains state, loading/menu/death cannot capture, normal K/I/K-prime transitions still run once, dock start still works, repeated press/hold/release does not duplicate capture, Test Play entry/exit remains intact. Rollback is removal of the added guard only. No test has been claimed PASS by this review.

OWNER APPROVAL REQUIRED BEFORE EDITING: approve the exact added guard in EasyNewtonFinaleController.Update above if desired. This report does not apply it or infer approval from the master prompt.

## Post-change scope check at review time

Git diff for existing Assets/Script/Contrast and Assets/Editor was empty. Existing protected runtime/Editor sources were unchanged at this check. New diagnostics require compilation and runtime evidence; no PASS implied. Final harness source was not yet present and remains UNVERIFIED until reviewed.

## Logging limitations requiring honest reporting

HTML emits event=frame; Unity emits eventName=object_sample. Comparator must normalize explicitly or observer schemas should be aligned without touching protected methods. LateUpdate displacement is observed final-frame delta, not proof of the internal commit vector; existing phase log records supply provenance. Keyboard snapshot is physical input, while replay adapter requires its own collected/consumed input evidence. HTML object ids use source/index mapping; Newton rails need stable role mapping. Initialization at observer installation can occur after autostart; retain the initial state snapshot. These are diagnostic completeness issues, not reasons to modify Core without approval.
