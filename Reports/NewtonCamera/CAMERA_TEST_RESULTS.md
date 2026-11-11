# Bidirectional Reversible Newton Camera Test Results (`CAMERA_TEST_RESULTS.md`)

## 1. Automated & Stress Verification Summary

All camera threshold scenarios (`CAM01`–`CAM05`), exact equality hysteresis tests, 120-reversal stress tests across variable dwell intervals (`1` to `25` frames per crossing), scale bound invariant checks (`minScale <= CurrentOrthographicSize <= maxScale`), and `L01`/`L02`/`L03` HTML parity regression tests passed with **0 failures**.

- **Runtime Trace Artifact:** `Reports/NewtonCamera/CAMERA_REVERSAL_TRACE.csv` (`1,502` lines: header + `1,501` runtime frames recorded across `CAM01`–`CAM05` and `120` rapid reversals).

---

## 2. Scenario Test Matrix (`F4`–`F7`, `G`, `H`)

| Test ID | Scenario | Verification Criteria | Result |
| --- | --- | --- | --- |
| `Cam_F4_00_PreNewtonPlatformFound` | Pre-Newton Platform Lookup | Identifies platform `13` (`pos = (67.617188, -9.640625)`, `size = (1.953125, 0.8125)`) in `L02` | **PASS** |
| `Cam_F4_00_ThresholdXMatchesPlatformLeftEdge` | Boundary Coordinate | `thresholdX == 66.6406255 == preNewtonPlatform.GetAabb().xMin` | **PASS** |
| `CAM01_TargetModeIsFollowPlayer` | `CAM01` (`playerWorldX < thresholdX`) | `TargetCameraMode == NormalPlatformingFollow` | **PASS** |
| `CAM01_ScaleAtMinScale` | `CAM01` (`playerWorldX < thresholdX`) | `CurrentOrthographicSize == 5.000 == minScale` | **PASS** |
| `CAM01_FollowsPlayer` | `CAM01` (`playerWorldX < thresholdX`) | `CurrentCameraPosition` matches `CalculateCurrentPlayerFollowTarget()` | **PASS** |
| `CAM_Equality_PreservesFollowPlayer` | Boundary Hysteresis (`playerWorldX == thresholdX`) | Exact equality while in `FollowPlayer` preserves `FollowPlayer` | **PASS** |
| `CAM02_TargetModeIsNewtonWide` | `CAM02` (`playerWorldX > thresholdX`) | Crossing right switches `TargetCameraMode` to `NewtonFixed` (`NewtonWide`) | **PASS** |
| `CAM02_GradualZoomOutStarts` | `CAM02` (`playerWorldX > thresholdX`) | `CurrentOrthographicSize` increases smoothly (`5.000 -> 5.029`) without jumping to `8.600` | **PASS** |
| `CAM02_CenterMovesProgressivelyNoTeleport` | `CAM02` (`playerWorldX > thresholdX`) | `CurrentCameraPosition` moves progressively toward `(79.84375, -8.25000)` (`step < 1.0 u`) | **PASS** |
| `CAM_Equality_PreservesNewtonWide` | Boundary Hysteresis (`playerWorldX == thresholdX`) | Exact equality while in `NewtonWide` preserves `NewtonWide` | **PASS** |
| `CAM03_MonotonicConvergenceToNewton` | `CAM03` (Full Convergence Right) | Monotonic zoom-out and monotonic distance reduction to `newtonFixedPosition` | **PASS** |
| `CAM03_ReachesMaxScale` | `CAM03` (Full Convergence Right) | Converges to `maxScale = 8.600` (`Diff: 0.00E+000`) | **PASS** |
| `CAM03_ReachesNewtonCenter` | `CAM03` (Full Convergence Right) | Converges to `(79.84375, -8.25000)` (`Dist: 0.00E+000`) | **PASS** |
| `CAM03_AllThreeTargetsVisibleAtMaxScale` | `CAM03` Viewport Framing | All 3 Newton targets (`K`, `I`, `K'`) are simultaneously inside viewport at `maxScale` | **PASS** |
| `CAM04_TargetModeIsFollowPlayer` | `CAM04` (`playerWorldX < thresholdX`) | Crossing left switches `TargetCameraMode` back to `FollowPlayer` | **PASS** |
| `CAM04_GradualZoomInStarts` | `CAM04` (`playerWorldX < thresholdX`) | `CurrentOrthographicSize` decreases smoothly (`8.600 -> 8.571`) without snapping to `5.000` | **PASS** |
| `CAM04_CenterMovesTowardPlayerNoSnap` | `CAM04` (`playerWorldX < thresholdX`) | `CurrentCameraPosition` moves progressively toward Player without snapping | **PASS** |
| `CAM05_ReachesMinScale` | `CAM05` (Full Convergence Left) | Converges to `minScale = 5.000` (`Diff: 0.00E+000`) | **PASS** |
| `CAM05_FollowsCurrentPlayerPosition` | `CAM05` (Moving Player Return) | Converges to the moving Player's live position `(63.57, -8.73)` (`Dist: 0.00E+000`) | **PASS** |
| `CAM05_NoFinalFrameSnap` | `CAM05` (Final-Frame Continuity) | Zero final-frame position snap when completing return (`maxStep = 0.7245 u`) | **PASS** |
| `Stress_F6_01_ScaleBoundsRespectedAcross120Reversals` | `CAM06`–`CAM10` (120 Reversals) | `5.00 <= CurrentOrthographicSize <= 8.60` on every single frame across 120 reversals (`1,501` frames) | **PASS** |
| `Stress_F5_01_ContinuousPositionAndScaleAcross120Reversals` | `CAM06`–`CAM10` (120 Reversals) | Zero position jumps (`maxStep <= 2.5 u`) and zero scale jumps (`maxScaleStep <= 0.6`) across 120 reversals | **PASS** |
| `Stress_F5_02_NeverRestartsFromEndpointOnReversal` | `CAM06`–`CAM10` (Mid-Transition Reversals) | Every mid-transition reversal continues from the actual in-flight camera state, never resetting to `minScale` or `maxScale` | **PASS** |
| `Trace_G_01_CsvGenerated` | `Task G` Trace Export | `Reports/NewtonCamera/CAMERA_REVERSAL_TRACE.csv` generated with `1,501` runtime frames | **PASS** |

---

## 3. Sample Excerpt from `Reports/NewtonCamera/CAMERA_REVERSAL_TRACE.csv`

```csv
Frame,DeltaTime,PlayerWorldX,PlatformLeftBoundaryX,TargetCameraMode,CurrentCameraPosX,CurrentCameraPosY,DesiredCameraPosX,DesiredCameraPosY,CurrentOrthographicSize,DesiredOrthographicSize,MinScale,MaxScale,PositionVelocityX,PositionVelocityY,ScaleVelocity,ReversedThisFrame
1,0.01667,66.14063,66.64063,FollowPlayer,66.14063,-8.73438,66.14063,-8.73438,5.00000,5.00000,5.00000,8.60000,0.00000,0.00000,0.00000,False
2,0.01667,67.44063,66.64063,NewtonWide,66.24966,-8.73052,79.84375,-8.25000,5.02865,8.60000,5.00000,8.60000,12.79444,0.45225,3.36128,True
3,0.01667,67.44063,66.64063,NewtonWide,66.54449,-8.72010,79.84375,-8.25000,5.10610,8.60000,5.00000,8.60000,22.39876,0.79175,5.88446,False
4,0.01667,67.44063,66.64063,NewtonWide,66.97708,-8.70481,79.84375,-8.25000,5.21975,8.60000,5.00000,8.60000,29.40952,1.03956,7.72629,False
5,0.01667,67.44063,66.64063,NewtonWide,67.50851,-8.68602,79.84375,-8.25000,5.35936,8.60000,5.00000,8.60000,34.32413,1.21328,9.01743,False
```
