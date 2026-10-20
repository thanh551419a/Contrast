using Contrast.Data;
using Contrast.UI;
using UnityEngine;

namespace Contrast.Level.EditorInteraction
{
    /// <summary>
    /// Coordinates trajectory editing operations, decoupling trajectory interaction logic
    /// from the monolithic LevelEditorController.
    /// Manages interaction state, hit testing, preview/commit/rollback transactions,
    /// and visual synchronization with the Inspector and Viewport.
    /// </summary>
    public sealed class TrajectoryEditController
    {
        private readonly TrajectoryInteractionState interactionState;
        private readonly TrajectoryHandleVisualizer visualizer;

        private ColorPlatform currentPlatform;

        public bool IsDragging => interactionState.IsDragging;
        public TrajectoryHandleKind ActiveHandle => interactionState.ActiveHandle;
        public TrajectoryInteractionState State => interactionState;
        public TrajectoryHandleVisualizer Visualizer => visualizer;

        public TrajectoryEditController(float handleSize = 0.16f, float handleHitRadius = 0.35f)
        {
            interactionState = new TrajectoryInteractionState();
            visualizer = new TrajectoryHandleVisualizer(handleSize, handleHitRadius);
        }

        public void Initialize(Transform handleParent)
        {
            visualizer.EnsureInitialized(handleParent);
        }

        public void SetTarget(ColorPlatform platform, LevelEditorUI ui = null)
        {
            if (currentPlatform != platform)
            {
                if (interactionState.IsDragging)
                {
                    CancelDrag(ui);
                }

                currentPlatform = platform;
                interactionState.Reset();
            }

            if (currentPlatform != null)
            {
                EnsurePlatformOnTrajectory(ui);
            }

            RefreshVisualization(true);
        }

        public void ClearTarget(LevelEditorUI ui = null)
        {
            if (interactionState.IsDragging)
            {
                CancelDrag(ui);
            }

            currentPlatform = null;
            interactionState.Reset();
            visualizer.HideAll();
        }

        // ── Hit Testing ──────────────────────────────────────────────

        public TrajectoryHandleKind HitHandle(Vector2 worldPos)
        {
            if (currentPlatform == null)
                return TrajectoryHandleKind.None;

            Trajectory traj = currentPlatform.GetTrajectory();
            TrajectoryData cfg = traj != null ? traj.Config : null;
            return visualizer.HitTest(worldPos, cfg);
        }

        // ── Viewport Drag Lifecycle: Preview, Commit, Rollback ────────

        public bool BeginDrag(TrajectoryHandleKind handle, Vector2 worldPos)
        {
            if (currentPlatform == null || !handle.IsTrajectoryHandle())
                return false;

            return interactionState.BeginDrag(handle, worldPos, currentPlatform);
        }

        public void UpdateDrag(Vector2 worldPos, LevelEditorUI ui)
        {
            if (!interactionState.IsDragging || currentPlatform == null)
                return;

            if (interactionState.CalculatePreview(worldPos, out Vector2 previewPlatformPos))
            {
                // Apply temporary preview state to the platform and component
                Trajectory traj = currentPlatform.GetTrajectory();
                if (traj != null && interactionState.WorkingPreviewConfig != null)
                {
                    traj.SetConfig(interactionState.WorkingPreviewConfig);
                }

                currentPlatform.SetEditorData(
                    previewPlatformPos,
                    currentPlatform.Size,
                    currentPlatform.RawGrayscaleColor);

                // Update visualizer and Inspector UI with preview data
                visualizer.UpdateVisualization(interactionState.WorkingPreviewConfig, true);
                ui?.RefreshSelectedPlatform(currentPlatform);
            }
        }

        public void CommitDrag(LevelEditorUI ui)
        {
            if (!interactionState.IsDragging)
                return;

            TrajectoryData finalConfig = interactionState.Commit(out _);
            visualizer.UpdateVisualization(finalConfig, true);
            ui?.RefreshSelectedPlatform(currentPlatform);
        }

        public void CancelDrag(LevelEditorUI ui)
        {
            if (!interactionState.IsDragging)
                return;

            TrajectoryData restoredConfig = interactionState.Rollback(out _);
            visualizer.UpdateVisualization(restoredConfig, true);
            ui?.RefreshSelectedPlatform(currentPlatform);
        }

        // ── Platform Lockstep Synchronization ────────────────────────

        /// <summary>
        /// Translates trajectory origin and spatial bounds in lockstep when the platform body
        /// or resize handles move.
        /// </summary>
        public void ApplyLockstepOffset(
            Vector2 initialOrigin,
            Vector2 initialBoundsMin,
            Vector2 initialBoundsMax,
            Vector2 delta,
            LevelEditorUI ui)
        {
            if (currentPlatform == null)
                return;

            Trajectory traj = currentPlatform.GetTrajectory();
            TrajectoryData cfg = traj != null ? traj.Config : null;
            if (cfg == null || !cfg.enabled)
                return;

            cfg.originX = initialOrigin.x + delta.x;
            cfg.originY = initialOrigin.y + delta.y;

            if (cfg.domainMode == TrajectoryDomainMode.SpatialBounds)
            {
                cfg.boundsXMin = initialBoundsMin.x + delta.x;
                cfg.boundsXMax = initialBoundsMax.x + delta.x;
                cfg.boundsYMin = initialBoundsMin.y + delta.y;
                cfg.boundsYMax = initialBoundsMax.y + delta.y;
            }

            visualizer.UpdateVisualization(cfg, true);
            ui?.RefreshSelectedPlatform(currentPlatform);
        }

        // ── UI-Driven Edits ──────────────────────────────────────────

        public void ApplyFromUI(TrajectoryData data, LevelEditorUI ui)
        {
            if (currentPlatform == null)
                return;

            Trajectory traj = currentPlatform.GetTrajectory();
            if (traj == null)
            {
                currentPlatform.InitializeTrajectory(data);
            }
            else
            {
                traj.SetConfig(data != null ? data.Clone() : null);
            }

            if (data != null && data.enabled && Trajectory.Validate(data) == null)
            {
                Vector2 onTraj = Trajectory.ProjectPointOntoTrajectory(data, currentPlatform.Position, out _);
                currentPlatform.SetEditorData(onTraj, currentPlatform.Size, currentPlatform.RawGrayscaleColor);
                ui?.RefreshSelectedPlatform(currentPlatform);
            }

            RefreshVisualization(true);
        }

        // ── Idle Constraint Enforcement ──────────────────────────────

        /// <summary>
        /// Keeps the platform anchored to its trajectory curve while idle in edit mode.
        /// </summary>
        public void EnsurePlatformOnTrajectory(LevelEditorUI ui = null)
        {
            if (currentPlatform == null || interactionState.IsDragging)
                return;

            Trajectory traj = currentPlatform.GetTrajectory();
            TrajectoryData cfg = traj != null ? traj.Config : null;
            if (cfg != null && cfg.enabled && Trajectory.Validate(cfg) == null)
            {
                Vector2 onTraj = Trajectory.ProjectPointOntoTrajectory(cfg, currentPlatform.Position, out _);
                if ((onTraj - currentPlatform.Position).sqrMagnitude > 1e-6f)
                {
                    currentPlatform.SetEditorData(onTraj, currentPlatform.Size, currentPlatform.RawGrayscaleColor);
                    ui?.RefreshSelectedPlatform(currentPlatform);
                }
            }
        }

        public void RefreshVisualization(bool isEditing)
        {
            if (currentPlatform == null)
            {
                visualizer.HideAll();
                return;
            }

            Trajectory traj = currentPlatform.GetTrajectory();
            TrajectoryData cfg = traj != null ? traj.Config : null;
            visualizer.UpdateVisualization(cfg, isEditing);
        }

        public void Cleanup()
        {
            visualizer.Cleanup();
            interactionState.Reset();
            currentPlatform = null;
        }
    }
}
