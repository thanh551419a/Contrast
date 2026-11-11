# Guardian review and protected approval requests

All seven configured specialists participated. Existing Core inventory remains unapproved. Review classified the existing Unity observer/replay as compatible non-Core diagnostics/test support; it does not certify overall architecture compliance. Existing Assets/Script and Assets/Editor diff was empty. No protected source edits applied.

## Request 1: authored collision dimensions — UNAPPLIED

Files/methods: Assets/Script/Contrast/Player/PlayerController.cs new SetRuntimeGeometry; Assets/Script/Contrast/Level/LevelLoader.cs BuildRuntimeLevel initial-player block.

Current: playerSize defaults1x1 (PlayerController:19); loader only respawns (:314-316). Actual canonical replay reports1x1, JSON requires.59375x1. Proposed: apply validated authored collision size before existing Respawn, no new frame writer.

```diff
--- a/Assets/Script/Contrast/Player/PlayerController.cs
+++ b/Assets/Script/Contrast/Player/PlayerController.cs
@@
         public Vector2 PlayerSize => playerSize;
         public Vector2 PlayerOffset => playerOffset;
+
+        public void SetRuntimeGeometry(Vector2 size)
+        {
+            playerSize = new Vector2(
+                Mathf.Max(0.0001f, Mathf.Abs(size.x)),
+                Mathf.Max(0.0001f, Mathf.Abs(size.y)));
+        }
+
         public ColorPlatform GroundedPlatform { get; private set; }
--- a/Assets/Script/Contrast/Level/LevelLoader.cs
+++ b/Assets/Script/Contrast/Level/LevelLoader.cs
@@
-                player.Respawn(
+                player.SetRuntimeGeometry(
+                    startData.size != null
+                        ? startData.size.ToVector2()
+                        : Vector2.one);
+
+                player.Respawn(
                     CurrentSpawnPosition,
                     CurrentSpawnColor);
```

Dependencies: all PlayerSize readers, overlap threshold calculations, support/feet/death and marker contact. Shared builder is used by Editor/Test Play; no Editor source edits. Regression: L01 dimensions/spawn/grounding, horizontal stops, jump/landing, partial/full overlap, checkpoints, rail support, legacy missing sizes, repeated loading and Editor Test Play entry/exit. Rollback removes both additions. This collision-only proposal does not fix root .3 render scale or PlayerVisualPresentation cached child geometry; those need separate precise scope.

## Request 2: Newton outside Playing — UNAPPLIED

File Assets/Script/Contrast/Level/EasyNewtonFinaleController.cs, Update:782. Current independent Update can reach dock/reveal/capture without GameManager state gate; main GameManager pipeline stops outside Playing. Source-confirmed reachability problem; no paused-capture runtime reproduction claimed.

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
```

Dependencies: all existing downstream dock/capture/reveal/feedback logic; Test Play state sharing. Regression: normal K/I/K-prime flow; pause capture/reveal, resume without lost state, loading/menu/death handling, press/hold/release once, Test Play entry/exit. Rollback removes guard. This does not integrate Newton into approved phases or repair activation geometry, stale input provenance, L02 dock configuration or completion state.

Owner approval required for each exact named span before applying. Guardian cannot approve itself. Governing project skill .agents/skills/contrast-guardian-gate/SKILL.md step4: “STOP and request the owner's explicit approval naming each method/span.” Architecture Contract AC-015/016 imposes the same gate; the master prompt explicitly preserves it.

Missing collectible construction/consumption requires a separate runtime-only exact design touching BuildRuntimeLevel/ProcessPlayerInteractions and lifecycle. Do not add independent Update or casually extend shared Editor marker enums. Checkpoint schema, death/camera, jump/phase, trajectory/platform corrections likewise require fresh protected proposals; the two proposals above are not blanket approval.

## Final diagnostic scope review
Guardian compared HTML to html.before-observer-repair.html: exactly two diagnostic lines changed (numeric support lookup, clear initialization snapshot). Compatible non-Core repair; no gameplay/input/platform writes. Six browser reruns succeeded; existing Unity protected source diff remains empty.

Owner replied “apply all,” authorizing the two named diffs above. They were applied exactly. Final diff scope check: only PlayerController.SetRuntimeGeometry, LevelLoader initial player geometry call, and Newton Update state guard. No Assets/Editor changes. Runtime build passed; see FINAL_REPORT.md for evidence/remaining risks.
